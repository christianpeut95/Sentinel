using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.EntityFrameworkCore;
using Sentinel.Data;
using Sentinel.Models;
using Sentinel.Services;
using Sentinel.Middleware;
using AntDesign;
using System.Text.Json;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.DataProtection;
using System.Threading.RateLimiting;
using Serilog;
using Serilog.Events;
using Serilog.Formatting.Compact;
using Serilog.Sinks.MSSqlServer;
using Sentinel.Extensions;

// Configure Serilog before creating the builder (file logging only for now)
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
    .MinimumLevel.Override("Microsoft.Hosting.Lifetime", LogEventLevel.Information)
    .MinimumLevel.Override("System", LogEventLevel.Warning)
    .Enrich.FromLogContext()
    .Enrich.WithMachineName()
    .Enrich.WithThreadId()
    .Enrich.WithEnvironmentName()
    .WriteTo.Console()
    .WriteTo.File(
        new CompactJsonFormatter(),
        path: "logs/sentinel-.json",
        rollingInterval: RollingInterval.Day,
        retainedFileCountLimit: 30,
        fileSizeLimitBytes: 100_000_000, // 100MB
        rollOnFileSizeLimit: true)
    .WriteTo.File(
        path: "logs/sentinel-.txt",
        rollingInterval: RollingInterval.Day,
        retainedFileCountLimit: 30,
        outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj}{NewLine}{Exception}")
    .CreateLogger();

var builder = WebApplication.CreateBuilder(args);

var useForwardedHeaders = builder.Configuration.GetValue<bool>("ReverseProxy:UseForwardedHeaders");
if (useForwardedHeaders)
{
    // Docker publishes only Caddy. The private Sentinel service therefore
    // accepts forwarded scheme/client information from that internal proxy.
    // Do not enable this setting when Kestrel is published directly.
    builder.Services.Configure<ForwardedHeadersOptions>(options =>
    {
        options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
        options.KnownNetworks.Clear();
        options.KnownProxies.Clear();
    });
}

// Use Serilog for all logging
builder.Host.UseSerilog();

// Environment variable overrides for sensitive configuration
// Priority: Environment Variables > appsettings.json
// This allows secrets to be injected at runtime without storing in config files

// Override connection string if environment variable exists
var envConnectionString = Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection");
if (!string.IsNullOrEmpty(envConnectionString))
{
    builder.Configuration["ConnectionStrings:DefaultConnection"] = envConnectionString;
}

// Override geocoding API key if environment variable exists
var envGeocodingApiKey = Environment.GetEnvironmentVariable("Geocoding__ApiKey");
if (!string.IsNullOrEmpty(envGeocodingApiKey))
{
    builder.Configuration["Geocoding:ApiKey"] = envGeocodingApiKey;
}

// Override geocoding email if environment variable exists
var envGeocodingEmail = Environment.GetEnvironmentVariable("Geocoding__Email");
if (!string.IsNullOrEmpty(envGeocodingEmail))
{
    builder.Configuration["Geocoding:Email"] = envGeocodingEmail;
}

// Keep the default request budget aligned with the protected-attachment limit.
// Jurisdiction shapefile endpoints explicitly opt into their larger 100 MB limit.
var configuredProtectedUploadBytes = builder.Configuration.GetValue<long?>("FileStorage:MaxUploadBytes");
var defaultMultipartRequestLimit = configuredProtectedUploadBytes is > 0
    ? configuredProtectedUploadBytes.Value
    : 26_214_400L;

builder.Services.Configure<KestrelServerOptions>(options =>
{
    options.Limits.MaxRequestBodySize = defaultMultipartRequestLimit;
    options.Limits.RequestHeadersTimeout = TimeSpan.FromMinutes(5);
});

// Configure Form options for multipart requests
builder.Services.Configure<Microsoft.AspNetCore.Http.Features.FormOptions>(options =>
{
    options.MultipartBodyLengthLimit = defaultMultipartRequestLimit;
    options.ValueLengthLimit = 1_048_576; // 1 MB per non-file form value
    options.MultipartHeadersLengthLimit = 16384;
    options.BufferBodyLengthLimit = defaultMultipartRequestLimit;
});

// Database
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

// Now that we have the connection string, add the SQL Server sink to Serilog.
// The isolated integration-test host deliberately has no SQL Server; it verifies
// authorization against an in-memory context instead.
var applicationLoggerConfiguration = new LoggerConfiguration()
    .MinimumLevel.Information()
    .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
    .MinimumLevel.Override("Microsoft.Hosting.Lifetime", LogEventLevel.Information)
    .MinimumLevel.Override("System", LogEventLevel.Warning)
    .Enrich.FromLogContext()
    .Enrich.WithMachineName()
    .Enrich.WithThreadId()
    .Enrich.WithEnvironmentName()
    .WriteTo.Console()
    .WriteTo.File(
        new CompactJsonFormatter(),
        path: "logs/sentinel-.json",
        rollingInterval: RollingInterval.Day,
        retainedFileCountLimit: 30,
        fileSizeLimitBytes: 100_000_000, // 100MB
        rollOnFileSizeLimit: true)
    .WriteTo.File(
        path: "logs/sentinel-.txt",
        rollingInterval: RollingInterval.Day,
        retainedFileCountLimit: 30,
        outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj}{NewLine}{Exception}");

if (!builder.Environment.IsEnvironment("Testing"))
{
    applicationLoggerConfiguration.WriteTo.MSSqlServer(
        connectionString: connectionString,
        sinkOptions: new MSSqlServerSinkOptions
        {
            TableName = "SentinelLogs",
            SchemaName = "dbo",
            AutoCreateSqlTable = true
        });
}

Log.Logger = applicationLoggerConfiguration.CreateLogger();

// Register the CaseCreationInterceptor (must be registered before DbContext)
builder.Services.AddSingleton<CaseCreationInterceptor>();

// Get command timeout from configuration once (used by both registrations)
var commandTimeout = builder.Configuration.GetValue<int?>("Database:CommandTimeoutSeconds") ?? 30;

// Register DbContext for Razor Pages and services (with interceptor)
// Use AddDbContext without serviceProvider lambda to avoid scoped DbContextOptions registration
builder.Services.AddDbContext<ApplicationDbContext>((serviceProvider, options) =>
{
    var caseInterceptor = serviceProvider.GetRequiredService<CaseCreationInterceptor>();

    options.UseSqlServer(connectionString, sqlOptions =>
    {
        sqlOptions.CommandTimeout(commandTimeout);
        sqlOptions.EnableRetryOnFailure(
            maxRetryCount: 5,
            maxRetryDelay: TimeSpan.FromSeconds(10),
            errorNumbersToAdd: null);
    })
    .AddInterceptors(caseInterceptor);
}, contextLifetime: ServiceLifetime.Scoped, optionsLifetime: ServiceLifetime.Singleton);

// Register DbContextFactory for Blazor components (needed for proper scoping in interactive scenarios)
// Use regular factory (not pooled) because ApplicationDbContext has multiple constructors
// and may need IHttpContextAccessor for audit logging
builder.Services.AddDbContextFactory<ApplicationDbContext>(options =>
{
    options.UseSqlServer(connectionString, sqlOptions =>
    {
        sqlOptions.CommandTimeout(commandTimeout);
        sqlOptions.EnableRetryOnFailure(
            maxRetryCount: 5,
            maxRetryDelay: TimeSpan.FromSeconds(10),
            errorNumbersToAdd: null);
    });
    // Note: Interceptors are NOT added to factory-created contexts (CaseCreationInterceptor is for Razor Pages only)
}, ServiceLifetime.Singleton);

// Identity (include roles so RoleManager and role stores are registered)
builder.Services.AddDefaultIdentity<ApplicationUser>(options =>
{
    options.SignIn.RequireConfirmedAccount = false;
    
    // Prefer long passwords plus common-password screening over arbitrary
    // composition rules. This keeps passwords compatible with password
    // managers and passphrases while the custom validator rejects known weak
    // passwords for every creation, change and reset flow.
    options.Password.RequiredLength = 12;
    options.Password.RequireDigit = false;
    options.Password.RequireLowercase = false;
    options.Password.RequireUppercase = false;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequiredUniqueChars = 0;
    
    // Lockout on failed attempts
    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
    options.Lockout.MaxFailedAccessAttempts = 5;
    options.Lockout.AllowedForNewUsers = true;
})
.AddRoles<IdentityRole>()
.AddEntityFrameworkStores<ApplicationDbContext>();

// Reject common passwords locally. The bundled deny-list is loaded once and
// applies through ASP.NET Identity to every creation, change and reset flow.
builder.Services.AddSingleton<ICommonPasswordDenyList, CommonPasswordDenyList>();
builder.Services.AddScoped<IPasswordValidator<ApplicationUser>, CommonPasswordValidator>();

// All Sentinel-owned cookies use the __Host- prefix. This requires HTTPS, a
// host-only cookie, and Path=/, which prevents another application on the
// same host from overriding an authentication-related cookie.
var cookieNameSuffix = builder.Configuration["Security:CookieNameSuffix"]?.Trim();
if (!string.IsNullOrEmpty(cookieNameSuffix) &&
    cookieNameSuffix.Any(character => !(char.IsAsciiLetterOrDigit(character) || character is '-' or '_')))
{
    throw new InvalidOperationException("Security:CookieNameSuffix may contain only letters, digits, hyphens, and underscores.");
}

string GetSecureCookieName(string name) => string.IsNullOrEmpty(cookieNameSuffix)
    ? name
    : $"{name}.{cookieNameSuffix}";

void ConfigureSecureHostCookie(CookieBuilder cookie, string name)
{
    cookie.Name = GetSecureCookieName(name);
    cookie.HttpOnly = true;
    cookie.SecurePolicy = CookieSecurePolicy.Always;
    cookie.SameSite = SameSiteMode.Lax;
    cookie.Path = "/";
}

// Configure authentication paths
builder.Services.ConfigureApplicationCookie(options =>
{
    ConfigureSecureHostCookie(options.Cookie, "__Host-Sentinel.Auth");
    options.LoginPath = "/Identity/Account/Login";
    options.LogoutPath = "/Identity/Account/Logout";
    options.AccessDeniedPath = "/Identity/Account/AccessDenied";

    // Browser navigation should still follow the ordinary Identity flow, but
    // APIs must return HTTP authorization statuses rather than an HTML login
    // redirect. Consumers can then reliably distinguish unauthenticated (401)
    // from unauthorized (403) requests without parsing a redirected page.
    options.Events.OnRedirectToLogin = context =>
    {
        if (context.Request.Path.StartsWithSegments("/api", StringComparison.OrdinalIgnoreCase))
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            return Task.CompletedTask;
        }

        context.Response.Redirect(context.RedirectUri);
        return Task.CompletedTask;
    };
    options.Events.OnRedirectToAccessDenied = context =>
    {
        if (context.Request.Path.StartsWithSegments("/api", StringComparison.OrdinalIgnoreCase))
        {
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            return Task.CompletedTask;
        }

        context.Response.Redirect(context.RedirectUri);
        return Task.CompletedTask;
    };
});

// Identity creates additional short-lived cookies for external sign-in and
// two-factor authentication. They carry no less sensitivity than the main
// application cookie, so apply the same transport and naming requirements.
builder.Services.Configure<Microsoft.AspNetCore.Authentication.Cookies.CookieAuthenticationOptions>(
    IdentityConstants.ExternalScheme,
    options => ConfigureSecureHostCookie(options.Cookie, "__Host-Sentinel.External"));
builder.Services.Configure<Microsoft.AspNetCore.Authentication.Cookies.CookieAuthenticationOptions>(
    IdentityConstants.TwoFactorRememberMeScheme,
    options => ConfigureSecureHostCookie(options.Cookie, "__Host-Sentinel.TwoFactorRememberMe"));
builder.Services.Configure<Microsoft.AspNetCore.Authentication.Cookies.CookieAuthenticationOptions>(
    IdentityConstants.TwoFactorUserIdScheme,
    options => ConfigureSecureHostCookie(options.Cookie, "__Host-Sentinel.TwoFactorUserId"));

// Revalidate the Identity security stamp on every authenticated request. Account
// lock/disable and access changes update the stamp so existing cookies are rejected
// immediately rather than remaining usable until their normal expiry.
builder.Services.Configure<SecurityStampValidatorOptions>(options =>
{
    options.ValidationInterval = TimeSpan.Zero;
});

// Authorization with custom permission policy provider
builder.Services.AddAuthorization();
builder.Services.AddSingleton<Microsoft.AspNetCore.Authorization.IAuthorizationPolicyProvider, Sentinel.Authorization.PermissionPolicyProvider>();
builder.Services.AddScoped<Microsoft.AspNetCore.Authorization.IAuthorizationHandler, Sentinel.Authorization.PermissionHandler>();

// Add claims transformation to populate permission claims for Razor views
builder.Services.AddScoped<Microsoft.AspNetCore.Authentication.IClaimsTransformation, Sentinel.Authorization.PermissionClaimsTransformation>();
builder.Services.AddScoped<IUserSessionInvalidationService, UserSessionInvalidationService>();

// Rate Limiting Configuration
builder.Services.AddRateLimiter(rateLimiterOptions =>
{
    // Tier 1: Highly Sensitive - Patient/Case PII Data (30 per minute)
    rateLimiterOptions.AddPolicy("sensitive-data", httpContext =>
    {
        var partitionKey = httpContext.User?.Identity?.Name ?? httpContext.Connection.RemoteIpAddress?.ToString() ?? "anonymous";
        return RateLimitPartition.GetFixedWindowLimiter(partitionKey, _ => new FixedWindowRateLimiterOptions
        {
            Window = TimeSpan.FromMinutes(1),
            PermitLimit = 30,
            QueueLimit = 0
        });
    });
    
    // Tier 1: Bulk Export - Large dataset queries (10 per hour)
    rateLimiterOptions.AddPolicy("bulk-export", httpContext =>
    {
        var partitionKey = httpContext.User?.Identity?.Name ?? httpContext.Connection.RemoteIpAddress?.ToString() ?? "anonymous";
        return RateLimitPartition.GetFixedWindowLimiter(partitionKey, _ => new FixedWindowRateLimiterOptions
        {
            Window = TimeSpan.FromHours(1),
            PermitLimit = 10,
            QueueLimit = 0
        });
    });
    
    // Tier 1: Bulk Export Moderate - (20 per hour)
    rateLimiterOptions.AddPolicy("bulk-export-moderate", httpContext =>
    {
        var partitionKey = httpContext.User?.Identity?.Name ?? httpContext.Connection.RemoteIpAddress?.ToString() ?? "anonymous";
        return RateLimitPartition.GetFixedWindowLimiter(partitionKey, _ => new FixedWindowRateLimiterOptions
        {
            Window = TimeSpan.FromHours(1),
            PermitLimit = 20,
            QueueLimit = 0
        });
    });
    
    // Tier 2: Moderate Sensitivity - Workflow/Tasks (100 per minute)
    rateLimiterOptions.AddPolicy("workflow-api", httpContext =>
    {
        var partitionKey = httpContext.User?.Identity?.Name ?? httpContext.Connection.RemoteIpAddress?.ToString() ?? "anonymous";
        return RateLimitPartition.GetFixedWindowLimiter(partitionKey, _ => new FixedWindowRateLimiterOptions
        {
            Window = TimeSpan.FromMinutes(1),
            PermitLimit = 100,
            QueueLimit = 2
        });
    });
    
    // Tier 2: Workflow - Lower frequency (60 per minute)
    rateLimiterOptions.AddPolicy("workflow-api-moderate", httpContext =>
    {
        var partitionKey = httpContext.User?.Identity?.Name ?? httpContext.Connection.RemoteIpAddress?.ToString() ?? "anonymous";
        return RateLimitPartition.GetFixedWindowLimiter(partitionKey, _ => new FixedWindowRateLimiterOptions
        {
            Window = TimeSpan.FromMinutes(1),
            PermitLimit = 60,
            QueueLimit = 2
        });
    });
    
    // Tier 3: Low Sensitivity - Lookups/Metadata (200 per minute)
    rateLimiterOptions.AddPolicy("lookup-api", httpContext =>
    {
        var partitionKey = httpContext.User?.Identity?.Name ?? httpContext.Connection.RemoteIpAddress?.ToString() ?? "anonymous";
        return RateLimitPartition.GetFixedWindowLimiter(partitionKey, _ => new FixedWindowRateLimiterOptions
        {
            Window = TimeSpan.FromMinutes(1),
            PermitLimit = 200,
            QueueLimit = 5
        });
    });

    // Password reset requests are anonymous and may trigger email delivery.
    // Keep this deliberately low to limit account-targeted mail abuse.
    rateLimiterOptions.AddPolicy("password-reset", httpContext =>
    {
        var partitionKey = httpContext.Connection.RemoteIpAddress?.ToString() ?? "anonymous";
        return RateLimitPartition.GetFixedWindowLimiter(partitionKey, _ => new FixedWindowRateLimiterOptions
        {
            Window = TimeSpan.FromMinutes(15),
            PermitLimit = 5,
            QueueLimit = 0
        });
    });

    rateLimiterOptions.AddPolicy("password-change", httpContext =>
    {
        var partitionKey = httpContext.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
            ?? httpContext.Connection.RemoteIpAddress?.ToString()
            ?? "anonymous";
        return RateLimitPartition.GetFixedWindowLimiter(partitionKey, _ => new FixedWindowRateLimiterOptions
        {
            Window = TimeSpan.FromMinutes(15),
            PermitLimit = 5,
            QueueLimit = 0
        });
    });

    // Global fallback (150 per minute per user)
    rateLimiterOptions.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(httpContext =>
    {
        var userId = httpContext.User?.Identity?.Name ?? httpContext.Connection.RemoteIpAddress?.ToString() ?? "anonymous";
        
        return RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: userId,
            factory: _ => new FixedWindowRateLimiterOptions
            {
                Window = TimeSpan.FromMinutes(1),
                PermitLimit = 150,
                QueueLimit = 0
            });
    });

    // Custom 429 response
    rateLimiterOptions.OnRejected = async (context, cancellationToken) =>
    {
        context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
        await context.HttpContext.Response.WriteAsJsonAsync(new
        {
            error = "Too many requests. Please slow down.",
            retryAfter = context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter) 
                ? retryAfter.TotalSeconds 
                : 60
        }, cancellationToken);
    };
});

// Add session services for TempData (needed for large bulk imports)
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    ConfigureSecureHostCookie(options.Cookie, "__Host-Sentinel.Session");
    options.Cookie.IsEssential = true;
});

// The antiforgery token is supplied in forms/headers, so its cookie can remain
// HttpOnly. Giving it its own host-only, HTTPS-only name avoids cookie shadowing.
builder.Services.AddAntiforgery(options =>
{
    ConfigureSecureHostCookie(options.Cookie, "__Host-Sentinel.AntiForgery");
});

builder.Services.AddHsts(options =>
{
    // The public deployment terminates HTTPS at Caddy; retain HTTPS on return
    // visits for at least the ASVS Level 1 minimum period.
    options.MaxAge = TimeSpan.FromDays(365);
});

// Razor Pages with global authorization
builder.Services.AddRazorPages(options =>
{
    // Require authentication for all pages by default
    options.Conventions.AuthorizeFolder("/");
    
    // Only sign-in and account-recovery pages are public. Account provisioning is
    // performed by the protected setup wizard or by an authorised administrator.
    options.Conventions.AllowAnonymousToAreaPage("Identity", "/Account/Login");
    options.Conventions.AllowAnonymousToAreaPage("Identity", "/Account/ForgotPassword");
    options.Conventions.AllowAnonymousToAreaPage("Identity", "/Account/ResetPassword");
})
.AddSessionStateTempDataProvider(); // Use session instead of cookies for TempData

// API Controllers (for AJAX endpoints). Cookie-authenticated mutations must
// validate antiforgery tokens just like Razor Page forms do. Individual
// endpoints can explicitly opt out only when they use a non-cookie trust model.
builder.Services.AddControllers(options =>
{
    options.Filters.Add(new Microsoft.AspNetCore.Mvc.AutoValidateAntiforgeryTokenAttribute());
});

// Blazor Server (for interactive settings/components)
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// ── Encryption & Email Services ────────────────────────
// Data Protection for encrypting sensitive configuration. Docker provides a
// protected, named volume through DataProtection:KeyRingPath so cookies and
// encrypted settings survive a container replacement.
var dataProtection = builder.Services.AddDataProtection().SetApplicationName("Sentinel");
var dataProtectionKeyRingPath = builder.Configuration["DataProtection:KeyRingPath"];
if (!string.IsNullOrWhiteSpace(dataProtectionKeyRingPath))
{
    Directory.CreateDirectory(dataProtectionKeyRingPath);
    dataProtection.PersistKeysToFileSystem(new DirectoryInfo(dataProtectionKeyRingPath));
}

// Encryption service for SMTP passwords and other secrets
builder.Services.AddSingleton<Sentinel.Services.IEncryptionService, Sentinel.Services.EncryptionService>();

// System settings service
builder.Services.AddScoped<Sentinel.Services.ISystemSettingsService, Sentinel.Services.SystemSettingsService>();
builder.Services.AddSingleton<Sentinel.Services.ISetupTokenFileService, Sentinel.Services.SetupTokenFileService>();
builder.Services.AddScoped<Sentinel.Services.ISetupWizardState, Sentinel.Services.SetupWizardState>();
builder.Services.AddScoped<Sentinel.Services.IWebDataRocksLicenseService, Sentinel.Services.WebDataRocksLicenseService>();
builder.Services.AddSingleton<Sentinel.Services.IApplicationVersionProvider, Sentinel.Services.ApplicationVersionProvider>();

// Telemetry service for privacy-safe logging and metrics
builder.Services.AddSingleton<Sentinel.Services.Telemetry.ITelemetryService, Sentinel.Services.Telemetry.TelemetryService>();

// Usage monitoring services
builder.Services.AddSingleton<Sentinel.Services.Telemetry.ActivityTracker>();
builder.Services.AddScoped<Sentinel.Services.Telemetry.UsageSnapshotBuilder>();
builder.Services.AddHttpClient<Sentinel.Services.Telemetry.UsageReportClient>();
builder.Services.AddHostedService<Sentinel.Services.Telemetry.UsageMonitoringHostedService>();

// Error reporting services
builder.Services.AddSingleton<Sentinel.Services.Telemetry.BreadcrumbTracker>();
builder.Services.AddHttpClient<Sentinel.Services.Telemetry.ErrorReportClient>();

// Email service - use Mock in Development, Smtp in Production
if (builder.Environment.IsDevelopment())
{
    builder.Services.AddScoped<Sentinel.Services.Email.IEmailService, Sentinel.Services.Email.MockEmailService>();
}
else
{
    builder.Services.AddScoped<Sentinel.Services.Email.IEmailService, Sentinel.Services.Email.SmtpEmailService>();
}

// app services
builder.Services.AddScoped<Sentinel.Services.IPatientDuplicateCheckService, Sentinel.Services.PatientDuplicateCheckService>();
builder.Services.AddScoped<Sentinel.Services.ILocationDuplicateCheckService, Sentinel.Services.LocationDuplicateCheckService>();
builder.Services.AddScoped<Sentinel.Services.IExposureRequirementService, Sentinel.Services.ExposureRequirementService>();
builder.Services.AddScoped<Sentinel.Services.IOccupationImportService, Sentinel.Services.OccupationImportService>();
builder.Services.AddScoped<Sentinel.Services.IAuditService, Sentinel.Services.AuditService>();
builder.Services.AddScoped<Sentinel.Services.IPatientCustomFieldService, Sentinel.Services.PatientCustomFieldService>();
builder.Services.AddScoped<Sentinel.Services.IPatientMergeService, Sentinel.Services.PatientMergeService>();
builder.Services.AddScoped<Sentinel.Services.IBackupService, Sentinel.Services.BackupService>();
builder.Services.AddScoped<Sentinel.Services.IPatientIdGeneratorService, Sentinel.Services.PatientIdGeneratorService>();
builder.Services.AddScoped<Sentinel.Services.ICaseIdGeneratorService, Sentinel.Services.CaseIdGeneratorService>();
builder.Services.AddScoped<Sentinel.Services.IPermissionService, Sentinel.Services.PermissionService>();
builder.Services.AddScoped<Sentinel.Services.IDiseaseAccessService, Sentinel.Services.DiseaseAccessService>();
builder.Services.AddScoped<Sentinel.Services.ICaseAccessService, Sentinel.Services.CaseAccessService>();
builder.Services.AddScoped<Sentinel.Services.IOutbreakAccessService, Sentinel.Services.OutbreakAccessService>();
builder.Services.AddSingleton<Sentinel.Services.IProtectedFileStorageService, Sentinel.Services.ProtectedFileStorageService>();
builder.Services.AddHostedService<Sentinel.Services.ProtectedFileStorageMigrationService>();
builder.Services.AddScoped<Sentinel.Services.CustomFieldService>();
builder.Services.AddScoped<Sentinel.Services.ITaskService, Sentinel.Services.TaskService>();
builder.Services.AddScoped<Sentinel.Services.ITaskAssignmentService, Sentinel.Services.TaskAssignmentService>();
builder.Services.AddScoped<Sentinel.Services.ISurveyService, Sentinel.Services.SurveyService>();
builder.Services.AddScoped<Sentinel.Services.IOutbreakService, Sentinel.Services.OutbreakService>();
builder.Services.AddScoped<Sentinel.Services.ILineListService, Sentinel.Services.LineListService>();
builder.Services.AddScoped<Sentinel.Services.IDuplicateDetectionService, Sentinel.Services.DuplicateDetectionService>();
builder.Services.AddScoped<Sentinel.Services.IJurisdictionService, Sentinel.Services.JurisdictionService>();
builder.Services.AddScoped<Sentinel.Services.Reporting.IReportFieldMetadataService, Sentinel.Services.Reporting.ReportFieldMetadataService>();
builder.Services.AddScoped<Sentinel.Services.Reporting.IReportDataAccessService, Sentinel.Services.Reporting.ReportDataAccessService>();
builder.Services.AddScoped<Sentinel.Services.Reporting.IReportDataService, Sentinel.Services.Reporting.ReportDataService>();
builder.Services.AddScoped<Sentinel.Services.Reporting.CollectionQueryFilterBuilder>();

// Feedback Services
builder.Services.AddScoped<Sentinel.Services.Telemetry.SystemInfoProvider>();
builder.Services.AddScoped<Sentinel.Services.Feedback.DiagnosticsBuilder>();
builder.Services.AddHttpClient<Sentinel.Services.Feedback.FeedbackApiClient>();
builder.Services.AddScoped<Sentinel.Services.Reporting.IReportFolderService, Sentinel.Services.Reporting.ReportFolderService>();
builder.Services.AddScoped<Sentinel.Services.Reporting.ICollectionMetadataService, Sentinel.Services.Reporting.CollectionMetadataService>();
builder.Services.AddScoped<Sentinel.Services.Reporting.IDynamicDateResolver, Sentinel.Services.Reporting.DynamicDateResolver>();
builder.Services.AddScoped<Sentinel.Services.IDataReviewService, Sentinel.Services.DataReviewService>();
builder.Services.AddScoped<Sentinel.Services.IDashboardService, Sentinel.Services.DashboardService>();

// Application Timezone Service (Singleton - same timezone for entire app)
builder.Services.AddSingleton<Sentinel.Services.IApplicationTimeZoneService, Sentinel.Services.ApplicationTimeZoneService>();

// Case Definition Evaluation Services
builder.Services.AddScoped<Sentinel.Services.CaseDefinitionEvaluation.OperatorEvaluator>();
builder.Services.AddScoped<Sentinel.Services.CaseDefinitionEvaluation.FieldResolver>();
builder.Services.AddScoped<Sentinel.Services.CaseDefinitionEvaluation.CriterionEvaluator>();
builder.Services.AddScoped<Sentinel.Services.CaseDefinitionEvaluation.CriteriaGroupEvaluator>();
builder.Services.AddScoped<Sentinel.Services.CaseDefinitionEvaluation.DefinitionEvaluator>();
builder.Services.AddScoped<Sentinel.Services.CaseDefinitionEvaluation.TreeBasedCriteriaEvaluator>();
builder.Services.AddScoped<Sentinel.Services.CaseDefinitionEvaluation.ICaseDefinitionEvaluationService, Sentinel.Services.CaseDefinitionEvaluation.CaseDefinitionEvaluationService>();

// HL7 Services
builder.Services.AddScoped<Sentinel.Services.HL7.IHL7ParserService, Sentinel.Services.HL7.HL7ParserService>();
builder.Services.AddScoped<Sentinel.Services.HL7.IDuplicateDetectionService, Sentinel.Services.HL7.DuplicateDetectionService>();
builder.Services.AddScoped<Sentinel.Services.HL7.IHL7FieldMappingService, Sentinel.Services.HL7.HL7FieldMappingService>();
builder.Services.AddScoped<Sentinel.Services.HL7.IHL7DataExtractionService, Sentinel.Services.HL7.HL7DataExtractionService>();
builder.Services.AddScoped<Sentinel.Services.HL7.IHL7MarkerResolutionService, Sentinel.Services.HL7.HL7MarkerResolutionService>();
builder.Services.AddScoped<Sentinel.Services.HL7.ICaseDefinitionMatchingService, Sentinel.Services.HL7.CaseDefinitionMatchingService>();
builder.Services.AddScoped<Sentinel.Services.HL7.ICaseMatchingService, Sentinel.Services.HL7.CaseMatchingService>();
builder.Services.AddScoped<Sentinel.Services.HL7.CaseDefinitionSpecificityScorer>();
builder.Services.AddScoped<Sentinel.Services.HL7.HL7DiagnosticService>();
builder.Services.AddScoped<Sentinel.Services.HL7.HL7ReviewService>();
builder.Services.AddScoped<Sentinel.Services.HL7.IHL7TestMessageService, Sentinel.Services.HL7.HL7TestMessageService>();
// HL7 File Monitor Service must be Singleton so all parts of app see the same monitoring state
builder.Services.AddSingleton<Sentinel.Services.HL7.IHL7FileMonitorService, Sentinel.Services.HL7.HL7FileMonitorService>();

// HL7 File Monitor Background Service
builder.Services.AddHostedService<Sentinel.Services.HL7.HL7FileMonitorHostedService>();

// Case Evaluation Queue and Background Worker
builder.Services.AddSingleton<Sentinel.Services.CaseDefinitionEvaluation.ICaseEvaluationQueue, Sentinel.Services.CaseDefinitionEvaluation.CaseEvaluationQueue>();
builder.Services.AddHostedService<Sentinel.Services.CaseDefinitionEvaluation.CaseEvaluationWorker>();

builder.Services.AddScoped<Sentinel.Services.ISurveyMappingService, Sentinel.Services.SurveyMappingService>();
builder.Services.AddScoped<Sentinel.Services.ICollectionMappingService, Sentinel.Services.CollectionMappingService>();
builder.Services.AddScoped<Sentinel.Services.CollectionMappingValidationService>();
builder.Services.AddScoped<Sentinel.Services.IPatientAddressService, Sentinel.Services.PatientAddressService>();
builder.Services.AddScoped<Sentinel.Services.TestDataGeneratorService>();
builder.Services.AddScoped<Sentinel.Helpers.PermissionHelper>();

// HttpContextAccessor for audit logging
builder.Services.AddHttpContextAccessor();

// AntDesign
builder.Services.AddAntDesign();

// Unified Location Lookup Service (Geocoding + Address/Business Search)
// Provider selection based on configuration
var geocodingProvider = builder.Configuration["Geocoding:Provider"]?.ToLowerInvariant() ?? "google";

if (geocodingProvider == "nominatim")
{
    builder.Services.AddHttpClient<Sentinel.Services.ILocationLookupService, Sentinel.Services.NominatimLocationLookupService>(c =>
    {
        c.BaseAddress = new Uri("https://nominatim.openstreetmap.org/");
        c.DefaultRequestHeaders.Add("User-Agent", "SurveillanceMVP/1.0");
    });
}
else // Default to Google
{
    // GoogleLocationLookupService uses the current Geocoding v4 and Places
    // endpoints with absolute URLs and header-based API key authentication.
    // Do not configure the retired Maps API v3 base URL here.
    builder.Services.AddHttpClient<Sentinel.Services.ILocationLookupService, Sentinel.Services.GoogleLocationLookupService>();
}

// Legacy Geocoding Service (delegates to ILocationLookupService for backward compatibility)
builder.Services.AddScoped<Sentinel.Services.IGeocodingService, Sentinel.Services.GoogleGeocodingService>();

// Background Geocoding Queue Service
builder.Services.AddSingleton<Sentinel.Services.IGeocodingQueueService, Sentinel.Services.GeocodingQueueService>();
builder.Services.AddHostedService<Sentinel.Services.GeocodingBackgroundService>();

var app = builder.Build();

// Fail closed at startup if the required ASVS password deny-list is missing or
// incomplete instead of silently allowing password checks to be bypassed.
_ = app.Services.GetRequiredService<ICommonPasswordDenyList>();

// Middleware
if (useForwardedHeaders)
{
    // Must run before HSTS and HTTPS redirection so the request scheme from
    // Caddy is used rather than the private HTTP hop to Kestrel.
    app.UseForwardedHeaders();
}

// This must be the outermost application handler. It logs every exception that
// escapes an endpoint and replaces the response with a safe, traceable error.
// ErrorReportingMiddleware remains further in the pipeline so it can submit a
// separately sanitised error event before the exception reaches this handler.
app.UseGlobalExceptionHandler();

// Add an explicit UTF-8 charset to every textual response, including static
// CSS/JavaScript/SVG assets and hand-written endpoint responses.
app.UseMiddleware<Utf8ContentTypeMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseMigrationsEndPoint();
}
else
{
    app.UseHsts();
}

// HTML page requests can use the friendly not-found page. API requests must
// retain their original status/body (for example, JSON validation and
// antiforgery errors); re-executing an API POST through a page endpoint turns
// the useful response into an unrelated HTML/Blazor content-type error.
app.UseWhen(
    context => !context.Request.Path.StartsWithSegments("/api"),
    branch => branch.UseStatusCodePagesWithReExecute("/not-found"));

app.UseHttpsRedirection();

// Baseline browser hardening. SAMEORIGIN permits Sentinel's own embedded case
// subforms while preventing other sites from framing authenticated pages.
app.Use(async (context, next) =>
{
    context.Response.OnStarting(() =>
    {
        context.Response.Headers["X-Content-Type-Options"] = "nosniff";
        context.Response.Headers["X-Frame-Options"] = "SAMEORIGIN";
        context.Response.Headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
        context.Response.Headers["Permissions-Policy"] = "geolocation=(), camera=(), microphone=()";
        // Establish CSP protections that are compatible with the current Razor and
        // client-library model. A stricter script/style policy can follow once the
        // remaining inline and third-party CDN scripts have been migrated.
        context.Response.Headers["Content-Security-Policy"] =
            "base-uri 'self'; object-src 'none'; frame-ancestors 'self'; form-action 'self'";
        return Task.CompletedTask;
    });

    await next();
});

// Sensitive data used to be written under wwwroot/uploads and wwwroot/data.
// Deny those legacy paths before static-file middleware so stale files cannot
// be retrieved by a guessed URL during or after migration.
app.Use(async (context, next) =>
{
    if (context.Request.Path.StartsWithSegments("/uploads") ||
        context.Request.Path.StartsWithSegments("/data/timeline-entries") ||
        context.Request.Path.StartsWithSegments("/data/timeline-backups"))
    {
        context.Response.StatusCode = StatusCodes.Status404NotFound;
        return;
    }

    await next();
});

app.UseStaticFiles();

app.UseRouting();

app.UseSession(); // Add session middleware (must be before authentication)

// ── Setup Redirect Middleware ──────────────────────────────────
// Redirect all requests to /Setup if initial setup is not completed
// Must come AFTER UseRouting() but BEFORE UseAuthentication()
// The isolated integration-test host has no deployment setup state. Keeping
// that redirect out of this one test-only environment lets the tests exercise
// the endpoint's actual authentication/authorization response instead.
if (!app.Environment.IsEnvironment("Testing"))
{
    app.UseSetupRedirect();
}

app.UseRateLimiter();

// Page view tracking middleware for usage monitoring
app.UseMiddleware<Sentinel.Middleware.PageViewTrackingMiddleware>();

// Error reporting middleware (captures unhandled exceptions)
app.UseMiddleware<Sentinel.Middleware.ErrorReportingMiddleware>();

app.UseAuthentication();
// Enforce immediate session replacement at the application boundary as well as
// through ASP.NET Identity's configured security-stamp validator. A later
// successful sign-in rotates the stored stamp, so an older browser cookie is
// rejected before it can reach an endpoint.
app.UseMiddleware<UserSessionValidationMiddleware>();
app.UseAuthorization();

// Disease access control middleware - must come after authentication
app.UseMiddleware<Sentinel.Middleware.DiseaseAccessMiddleware>();

// Anti-forgery middleware (required for Blazor forms)
app.UseAntiforgery();

// Razor Pages routing
app.MapRazorPages();

// API Controllers routing
app.MapControllers();

// Blazor components with interactive server rendering
app.MapRazorComponents<Sentinel.Components.App>()
    .AddInteractiveServerRenderMode();

// Minimal API endpoint for address suggestions (returns display, lat, lon and address components)
app.MapGet("/api/address-suggest", async (HttpRequest req, Sentinel.Services.ILocationLookupService locationService) =>
{
    var q = req.Query["q"].ToString();
    var limitStr = req.Query["limit"].ToString();
    if (string.IsNullOrWhiteSpace(q))
        return Results.Json(Array.Empty<object>());

    var limit = 5;
    if (!string.IsNullOrWhiteSpace(limitStr) && int.TryParse(limitStr, out var parsed)) 
        limit = parsed;

    var results = await locationService.SearchAddressesAsync(q, limit);
    
    // Map to legacy format for existing clients
    var legacyFormat = results.Select(r => new 
    { 
        display = r.Display, 
        lat = r.Latitude, 
        lon = r.Longitude, 
        address = r.AddressComponents 
    });

    return Results.Json(legacyFormat);
}).RequireAuthorization("Permission.ReferenceData.View");

// Minimal API endpoint for place/business suggestions with location bias (for timeline feature)
app.MapGet("/api/places-suggest", async (HttpRequest req, Sentinel.Services.ILocationLookupService locationService) =>
{
    var q = req.Query["q"].ToString();
    var limitStr = req.Query["limit"].ToString();
    var latStr = req.Query["lat"].ToString();
    var lonStr = req.Query["lon"].ToString();

    if (string.IsNullOrWhiteSpace(q))
        return Results.Json(Array.Empty<object>());

    var limit = 5;
    if (!string.IsNullOrWhiteSpace(limitStr) && int.TryParse(limitStr, out var parsed)) 
        limit = parsed;

    double? lat = null, lon = null;
    if (!string.IsNullOrWhiteSpace(latStr) && double.TryParse(latStr, out var parsedLat))
        lat = parsedLat;
    if (!string.IsNullOrWhiteSpace(lonStr) && double.TryParse(lonStr, out var parsedLon))
        lon = parsedLon;

    var results = await locationService.SearchPlacesAsync(q, limit, lat, lon);

    // Format for timeline feature (matches expected JavaScript properties)
    var formattedResults = results.Select(r => new 
    { 
        placeId = r.PlaceId,
        displayName = r.Name,
        description = r.Name,  // Fallback for compatibility
        formattedAddress = r.Address,
        coordinates = new { lat = r.Latitude, lon = r.Longitude }
    });

    return Results.Json(formattedResults);
}).RequireAuthorization("Permission.ReferenceData.View");



// API endpoint for jurisdiction autocomplete
app.MapGet("/api/jurisdictions/search", async (string? term, int? typeId, ApplicationDbContext context) =>
{
    if (string.IsNullOrWhiteSpace(term) && !typeId.HasValue)
        return Results.Json(Array.Empty<object>());

    var query = context.Jurisdictions
        .Where(j => j.IsActive);

    // Filter by type if provided
    if (typeId.HasValue)
    {
        query = query.Where(j => j.JurisdictionTypeId == typeId.Value);
    }

    // Filter by search term if provided
    if (!string.IsNullOrWhiteSpace(term))
    {
        query = query.Where(j => j.Name.Contains(term) || 
                                (j.Code != null && j.Code.Contains(term)));
    }

    var jurisdictions = await query
        .OrderBy(j => j.DisplayOrder)
        .ThenBy(j => j.Name)
        .Take(50)
        .Select(j => new
        {
            Id = j.Id,
            Name = j.Name,
            Code = j.Code,
            JurisdictionTypeId = j.JurisdictionTypeId
        })
        .ToListAsync();

    return Results.Json(jurisdictions);
}).RequireAuthorization("Permission.ReferenceData.View");

// API endpoint for organization autocomplete
app.MapGet("/api/organizations/search", async (string term, ApplicationDbContext context) =>
{
    if (string.IsNullOrWhiteSpace(term))
        return Results.Json(Array.Empty<object>());

    var organizations = await context.Organizations
        .Where(o => o.IsActive && o.Name.Contains(term))
        .OrderBy(o => o.Name)
        .Take(20)
        .Select(o => new
        {
            Id = o.Id,
            Name = o.Name,
            ContactPerson = o.ContactPerson,
            Phone = o.Phone
        })
        .ToListAsync();

    return Results.Json(organizations);
}).RequireAuthorization("Permission.Organization.View");

// API endpoint to get lab results for a case
app.MapGet("/api/cases/{caseId}/lab-results", async (Guid caseId, ApplicationDbContext context, Sentinel.Services.ICaseAccessService caseAccessService) =>
{
    if (!await caseAccessService.CanAccessCaseAsync(caseId))
        return Results.NotFound();

    var labResults = await context.LabResults
        .Include(lr => lr.SpecimenType)
        .Include(lr => lr.ResultUnits)
        .Include(lr => lr.TestedDisease)
        .Include(lr => lr.Markers).ThenInclude(m => m.Pathogen)
        .Include(lr => lr.Markers).ThenInclude(m => m.TestMethod)
        .Where(lr => lr.CaseId == caseId)
        .OrderByDescending(lr => lr.SpecimenCollectionDate)
        .Select(lr => new
        {
            Id = lr.Id,
            FriendlyId = lr.FriendlyId,
            TestedDiseaseName = lr.TestedDisease != null ? lr.TestedDisease.Name : null,
            SpecimenTypeName = lr.SpecimenType != null ? lr.SpecimenType.Name : null,
            SpecimenCollectionDate = lr.SpecimenCollectionDate,
            ResultUnitsName = lr.ResultUnits != null ? lr.ResultUnits.Name : null,
            Markers = lr.Markers.Select(m => new
            {
                PathogenName = m.Pathogen != null ? m.Pathogen.Name : null,
                TestMethodName = m.TestMethod != null ? m.TestMethod.Name : null,
                QualitativeResult = m.QualitativeResultText,
                QuantitativeValue = m.QuantitativeValue,
                QuantitativeUnit = m.QuantitativeUnit,
                InterpretationFlag = m.InterpretationFlag
            }).ToList()
        })
        .ToListAsync();

    return Results.Json(labResults);
}).RequireAuthorization("Permission.Case.View");

// API endpoint to delete a lab result
app.MapDelete("/api/lab-results/{id}", async (Guid id, ApplicationDbContext context, Sentinel.Services.ICaseAccessService caseAccessService) =>
{
    var labResult = await context.LabResults
        .FirstOrDefaultAsync(lr => lr.Id == id);
    if (labResult?.CaseId is not Guid caseId || !await caseAccessService.CanAccessCaseAsync(caseId))
        return Results.NotFound();

    context.LabResults.Remove(labResult);
    await context.SaveChangesAsync();

    return Results.Ok();
}).RequireAuthorization("Permission.Laboratory.Delete")
  .RequireAuthorization("Permission.Case.Edit")
  .WithMetadata(new RequireAntiforgeryTokenAttribute(true))
  .RequireValidAntiforgery();

// API endpoint to get exposures for a case
app.MapGet("/api/cases/{caseId}/exposures", async (Guid caseId, ApplicationDbContext context, Sentinel.Services.ICaseAccessService caseAccessService) =>
{
    if (!await caseAccessService.CanAccessCaseAsync(caseId))
        return Results.NotFound();

    var exposures = await context.ExposureEvents
        .Include(e => e.Location)
        .Include(e => e.Event)
        .Where(e => e.ExposedCaseId == caseId)
        .OrderByDescending(e => e.ExposureStartDate)
        .Select(e => new
        {
            Id = e.Id,
            LocationName = e.Location != null ? e.Location.Name : null,
            EventName = e.Event != null ? e.Event.Name : null,
            ExposureStartDate = e.ExposureStartDate,
            ExposureEndDate = e.ExposureEndDate,
            ExposureType = e.ExposureType.ToString(),
            Description = e.Description
        })
        .ToListAsync();

    return Results.Json(exposures);
}).RequireAuthorization("Permission.Exposure.View");

// API endpoint to delete an exposure
app.MapDelete("/api/exposures/{id}", async (Guid id, ApplicationDbContext context, Sentinel.Services.ICaseAccessService caseAccessService) =>
{
    var exposure = await context.ExposureEvents
        .FirstOrDefaultAsync(e => e.Id == id);
    if (exposure == null || !await caseAccessService.CanAccessAllCasesAsync(
            exposure.SourceCaseId.HasValue
                ? new[] { exposure.ExposedCaseId, exposure.SourceCaseId.Value }
                : new[] { exposure.ExposedCaseId }))
        return Results.NotFound();

    context.ExposureEvents.Remove(exposure);
    await context.SaveChangesAsync();

    return Results.Ok();
}).RequireAuthorization("Permission.Exposure.Delete")
  .RequireAuthorization("Permission.Case.Edit")
  .WithMetadata(new RequireAntiforgeryTokenAttribute(true))
  .RequireValidAntiforgery();

// API endpoint to get patient address for a case
app.MapGet("/api/patients/{caseId}/address", async (Guid caseId, ApplicationDbContext context, Sentinel.Services.ICaseAccessService caseAccessService) =>
{
    if (!await caseAccessService.CanAccessCaseAsync(caseId))
        return Results.NotFound();

    var caseEntity = await context.Cases
        .Include(c => c.Patient)
        .FirstOrDefaultAsync(c => c.Id == caseId);

    if (caseEntity == null || caseEntity.Patient == null)
        return Results.NotFound();

    var patient = caseEntity.Patient;

    return Results.Json(new
    {
        addressLine = patient.AddressLine,
        city = patient.City,
        state = patient.State,
        postalCode = patient.PostalCode,
        country = "Australia", // Default if not stored
        latitude = patient.Latitude,
        longitude = patient.Longitude
    });
}).RequireAuthorization("Permission.Patient.View");

// API endpoint for user autocomplete (for task assignment)
app.MapGet("/api/users/search", async (string? term, Microsoft.AspNetCore.Identity.UserManager<ApplicationUser> userManager) =>
{
    if (string.IsNullOrWhiteSpace(term) || term.Trim().Length < 2)
        return Results.Json(Array.Empty<object>());

    var normalizedTerm = term.Trim().ToUpperInvariant();

    var users = userManager.Users
        .Where(u =>
            (u.UserName != null && u.UserName.ToUpper().Contains(normalizedTerm)) ||
            (u.Email != null && u.Email.ToUpper().Contains(normalizedTerm)))
        .OrderBy(u => u.UserName)
        .Take(20)
        .Select(u => new
        {
            id = u.Id,
            text = u.UserName ?? u.Email ?? "Unknown user",
            displayName = u.UserName ?? u.Email ?? "Unknown user"
        })
        .ToList();

    return Results.Json(users);
}).RequireAuthorization();

// API endpoint for disease exposure requirements
app.MapGet("/api/diseases/{id:guid}/exposure-requirements", async (Guid id, IExposureRequirementService service, ApplicationDbContext context) =>
{
    // Do not disclose restricted disease configuration through an otherwise
    // routine case-entry lookup.
    if (!await context.Diseases.AsNoTracking().AnyAsync(d => d.Id == id))
        return Results.NotFound();

    var disease = await service.GetRequirementsForDiseaseAsync(id);
    var shouldPrompt = await service.ShouldPromptForExposureAsync(id);

    return Results.Json(new 
    { 
        shouldPrompt = shouldPrompt,
        mode = disease?.ExposureTrackingMode.ToString(),
        guidanceText = disease?.ExposureGuidanceText,
        isRequired = disease?.ExposureTrackingMode == Sentinel.Models.ExposureTrackingMode.LocalSpecificRegion ||
                     disease?.ExposureTrackingMode == Sentinel.Models.ExposureTrackingMode.OverseasAcquired,
        defaultToResidential = disease?.DefaultToResidentialAddress ?? false,
        requireCoordinates = disease?.RequireGeographicCoordinates ?? false,
        allowDomestic = disease?.AllowDomesticAcquisition ?? true
    });
}).RequireAuthorization("Permission.Case.Create");

// Apply migrations and seed data on startup with retry logic. The test host
// replaces this context with an in-memory database and supplies its own data.
if (!app.Environment.IsEnvironment("Testing"))
{
    using var scope = app.Services.CreateScope();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    
    // Retry database initialization (SQL Server may take time to start in Docker)
    var maxRetries = 10;
    var retryDelay = TimeSpan.FromSeconds(5);
    
    for (int retry = 0; retry < maxRetries; retry++)
    {
        try
        {
            logger.LogInformation("Applying database migrations... (Attempt {Retry}/{MaxRetries})", retry + 1, maxRetries);
            
            // Auto-apply database migrations (creates DB if doesn't exist)
            await dbContext.Database.MigrateAsync();
            
            logger.LogInformation("Database migrations applied successfully");
            
            // Ensure reporting views are correctly created (idempotent - safe to run multiple times)
            logger.LogInformation("Verifying reporting views...");
            try
            {
                await EnsureReportingViewsExistAsync(dbContext, logger);
                logger.LogInformation("Reporting views verified successfully");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to create/verify reporting views. Report Builder may not function correctly.");
                // Don't throw - allow app to start even if views fail
            }
            
            break; // Success - exit retry loop
        }
        catch (Exception ex) when (retry < maxRetries - 1)
        {
            logger.LogWarning(ex, "Failed to apply migrations (Attempt {Retry}/{MaxRetries}). Retrying in {Delay} seconds...", 
                retry + 1, maxRetries, retryDelay.TotalSeconds);
            await Task.Delay(retryDelay);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to apply database migrations after {MaxRetries} attempts. Application may not function correctly.", maxRetries);
            throw;
        }
    }
    
    // Seed data
    logger.LogInformation("Seeding permissions and lookup data...");
    await Sentinel.Services.PermissionSeedService.SeedAsync(scope.ServiceProvider);
    await Sentinel.Services.LookupDataSeedService.SeedAsync(scope.ServiceProvider);
    logger.LogInformation("Data seeding complete");

    // Conditionally seed demo users (only when Demo:EnableDemoUsers is true)
    await Sentinel.Services.DemoUserSeedService.SeedAsync(scope.ServiceProvider);
}

// Health check endpoint for Docker and monitoring
app.MapGet("/health", async (ApplicationDbContext dbContext, ILoggerFactory loggerFactory) =>
{
    try
    {
        // Check database connectivity
        await dbContext.Database.CanConnectAsync();
        
        return Results.Ok(new
        {
            status = "healthy",
            application = "Sentinel",
            timestamp = DateTime.UtcNow,
            database = "connected"
        });
    }
    catch (Exception ex)
    {
        var errorMessage = UserFacingError.Create(
            loggerFactory.CreateLogger("Sentinel.Health"),
            ex,
            "health check");
        return Results.Json(new
        {
            status = "unhealthy",
            application = "Sentinel",
            timestamp = DateTime.UtcNow,
            database = "disconnected",
            error = errorMessage
        }, statusCode: 503);
    }
}).AllowAnonymous();

// Helper method to ensure reporting views exist and are correct
static async Task EnsureReportingViewsExistAsync(ApplicationDbContext dbContext, Microsoft.Extensions.Logging.ILogger logger)
{
    var scriptPath = Path.Combine(AppContext.BaseDirectory, "Scripts", "RecreateReportingViews.sql");
    
    if (!File.Exists(scriptPath))
    {
        logger.LogWarning("RecreateReportingViews.sql not found at {Path}. Skipping view recreation.", scriptPath);
        return;
    }
    
    var viewCreationSql = await File.ReadAllTextAsync(scriptPath);
    logger.LogInformation("Loaded view recreation script from {Path}", scriptPath);
    
    // Split by GO statements and execute each batch separately
    var batches = viewCreationSql.Split(new[] { "\r\nGO\r\n", "\nGO\n", "\r\nGO", "\nGO" }, StringSplitOptions.RemoveEmptyEntries);
    logger.LogInformation("Split into {Count} SQL batches", batches.Length);
    
    int executedBatches = 0;
    foreach (var batch in batches)
    {
        var trimmedBatch = batch.Trim();
        if (!string.IsNullOrWhiteSpace(trimmedBatch) && 
            !trimmedBatch.StartsWith("--") && 
            !trimmedBatch.StartsWith("PRINT"))
        {
            try
            {
                await dbContext.Database.ExecuteSqlRawAsync(trimmedBatch);
                executedBatches++;
                logger.LogDebug("Executed batch {Number}: {Preview}...", executedBatches, trimmedBatch.Substring(0, Math.Min(50, trimmedBatch.Length)));
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to execute SQL batch {Number}: {Batch}", executedBatches + 1, trimmedBatch.Substring(0, Math.Min(200, trimmedBatch.Length)));
                throw;
            }
        }
    }
    
    logger.LogInformation("Successfully executed {Count} SQL batches to recreate reporting views", executedBatches);
}

// Wire up the evaluation queue to ApplicationDbContext instances
// This allows the partial class to queue evaluations on SaveChangesAsync
using (var scope = app.Services.CreateScope())
{
    var queue = scope.ServiceProvider.GetRequiredService<Sentinel.Services.CaseDefinitionEvaluation.ICaseEvaluationQueue>();
    var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    dbContext.SetEvaluationQueue(queue);
}

// ╔══════════════════════════════════════════════════════════════════════╗
// ║ SETUP TOKEN GENERATION                                               ║
// ║ Generate a secure token on first run for initial setup access       ║
// ╚══════════════════════════════════════════════════════════════════════╝
// The isolated integration-test host must not create a setup token or file.
if (!app.Environment.IsEnvironment("Testing"))
{
    using var scope = app.Services.CreateScope();
    var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    var encryptionService = scope.ServiceProvider.GetRequiredService<Sentinel.Services.IEncryptionService>();
    var setupTokenFileService = scope.ServiceProvider.GetRequiredService<Sentinel.Services.ISetupTokenFileService>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

    try
    {
        // Check if SystemSettings table exists and has data
        var settings = await context.SystemSettings.FirstOrDefaultAsync();

        // Versions prior to 0.9.0-beta stored the plaintext token beside the
        // application binaries. It is invalid once this version rotates the
        // hash, but remove it during upgrade so it does not remain on disk.
        var legacyTokenFilePath = Path.Combine(AppContext.BaseDirectory, "setup-token.txt");
        if (File.Exists(legacyTokenFilePath))
        {
            try
            {
                File.Delete(legacyTokenFilePath);
                logger.LogInformation("Deleted legacy setup token file during startup");
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Could not remove the legacy setup token file; its token will not be accepted after rotation");
            }
        }

        if (settings == null)
        {
            // First-time setup - generate token
            logger.LogInformation("No system settings found - generating setup token for first-time setup");

            // Generate cryptographically secure random token (48 bytes = 64 base64 chars)
            var tokenBytes = new byte[48];
            using (var rng = System.Security.Cryptography.RandomNumberGenerator.Create())
            {
                rng.GetBytes(tokenBytes);
            }
            var plainToken = Convert.ToBase64String(tokenBytes);

            // Hash token for storage
            var hashedToken = encryptionService.Hash(plainToken);

            // Create system settings record
            settings = new Sentinel.Models.SystemSettings
            {
                Id = Guid.NewGuid(),
                SetupToken = hashedToken,
                SetupTokenGeneratedAt = DateTime.UtcNow,
                SetupTokenExpiresAt = DateTime.UtcNow.AddHours(48),
                IsSetupCompleted = false,
                EnforceHttps = true,
                SmtpEnableSsl = true,
                HL7ProcessingEnabled = false,
                SurveillanceStartupCompleted = false,
                SurveillanceStartupProgressPercentage = 0,
                EnableFeedbackWidget = false,
                EnableUsageMonitoring = false,
                TelemetryEnabled = false,
                CreatedAt = DateTime.UtcNow
            };

            // The plaintext token must never be sent to logs, the database,
            // or any web-served location. Store it in the protected operator
            // file before making its hash usable in the database.
            await setupTokenFileService.WriteTokenAsync(plainToken);
            try
            {
                context.SystemSettings.Add(settings);
                await context.SaveChangesAsync();
            }
            catch
            {
                await setupTokenFileService.DeleteTokenAsync();
                throw;
            }

            logger.LogCritical(
                "Initial Sentinel setup is pending. Retrieve the one-time setup token from the protected server file {TokenFilePath}. The token expires at {Expiry} UTC and is deleted after setup completes.",
                setupTokenFileService.TokenFilePath,
                settings.SetupTokenExpiresAt);
        }
        else if (!settings.IsSetupCompleted && settings.SetupToken != null)
        {
            // A token that is expired, or whose protected file was lost before
            // setup, cannot be used. Replace it automatically rather than
            // asking an operator to alter database state during commissioning.
            var tokenExpired = !settings.SetupTokenExpiresAt.HasValue ||
                settings.SetupTokenExpiresAt.Value <= DateTime.UtcNow;
            if (tokenExpired || !setupTokenFileService.TokenFileExists)
            {
                var replacementToken = Convert.ToBase64String(System.Security.Cryptography.RandomNumberGenerator.GetBytes(48));
                settings.SetupToken = encryptionService.Hash(replacementToken);
                settings.SetupTokenGeneratedAt = DateTime.UtcNow;
                settings.SetupTokenExpiresAt = DateTime.UtcNow.AddHours(48);

                await setupTokenFileService.WriteTokenAsync(replacementToken);
                try
                {
                    await context.SaveChangesAsync();
                }
                catch
                {
                    await setupTokenFileService.DeleteTokenAsync();
                    throw;
                }

                logger.LogCritical(
                    "Initial Sentinel setup remains incomplete. A replacement one-time setup token was created in the protected server file {TokenFilePath}; it expires at {Expiry} UTC.",
                    setupTokenFileService.TokenFilePath,
                    settings.SetupTokenExpiresAt);
            }
            else
            {
                // Setup in progress - remind the server operator where to obtain
                // the token without ever writing it into application logs.
                logger.LogWarning("Setup not completed. The one-time token remains in the protected server file {TokenFilePath}", setupTokenFileService.TokenFilePath);
                logger.LogWarning("Setup expires: {Expiry}", settings.SetupTokenExpiresAt);
            }
        }
        else if (settings.IsSetupCompleted)
        {
            logger.LogInformation("Sentinel setup completed at {CompletedAt}", settings.SetupCompletedAt);
        }

        // Ensure every installation has a stable, persisted identifier before hosted
        // services begin. This also repairs installations created before InstallationId
        // was introduced or where it was otherwise missing.
        var systemSettingsService = scope.ServiceProvider.GetRequiredService<Sentinel.Services.ISystemSettingsService>();
        await systemSettingsService.GetInstallationIdAsync();
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Failed to initialize system settings / setup token");
        // Don't throw - allow app to start, but setup wizard will handle missing settings
    }
}

app.Run();


