using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Sentinel.Data;
using Sentinel.Models;
using Sentinel.Services;
using Microsoft.Data.SqlClient;
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

var useForwardedHeaders = builder.ConfigureSentinelDeployment();

// Use Serilog for all logging
builder.Host.UseSerilog();

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
    try
    {
        await using var loggingConnection = new SqlConnection(connectionString);
        await loggingConnection.OpenAsync();

        applicationLoggerConfiguration.WriteTo.MSSqlServer(
            connectionString: connectionString,
            sinkOptions: new MSSqlServerSinkOptions
            {
                TableName = "SentinelLogs",
                SchemaName = "dbo",
                AutoCreateSqlTable = true
            });
    }
    catch (SqlException ex)
    {
        // A new LocalDB database is created by EF migrations later in startup.
        // Do not let a database-backed log sink prevent that first-run path.
        Log.Warning(ex, "SQL logging is unavailable during startup; using console and file logging until the database is available");
    }
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

// All standard Sentinel deployments use the __Host- prefix. This requires
// HTTPS, a host-only cookie, and Path=/, which prevents another application
// on the same host from overriding an authentication-related cookie.
//
// The installer has one deliberately narrow exception: its local-evaluation
// Compose file binds Sentinel to 127.0.0.1 only and opts into this setting.
// A browser cannot set a __Host- cookie over HTTP, so that mode uses ordinary
// host-only cookie names and SameAsRequest. It must never be enabled for a
// network-accessible or production deployment.
var cookieNameSuffix = builder.Configuration["Security:CookieNameSuffix"]?.Trim();
var localEvaluationMode = builder.Configuration.GetValue<bool>("Security:LocalEvaluationMode");
if (!string.IsNullOrEmpty(cookieNameSuffix) &&
    cookieNameSuffix.Any(character => !(char.IsAsciiLetterOrDigit(character) || character is '-' or '_')))
{
    throw new InvalidOperationException("Security:CookieNameSuffix may contain only letters, digits, hyphens, and underscores.");
}

string GetSentinelCookieName(string name)
{
    if (localEvaluationMode && name.StartsWith("__Host-", StringComparison.Ordinal))
    {
        name = name["__Host-".Length..];
    }

    return string.IsNullOrEmpty(cookieNameSuffix)
        ? name
        : $"{name}.{cookieNameSuffix}";
}

void ConfigureSentinelCookie(CookieBuilder cookie, string name)
{
    cookie.Name = GetSentinelCookieName(name);
    cookie.HttpOnly = true;
    cookie.SecurePolicy = localEvaluationMode
        ? CookieSecurePolicy.SameAsRequest
        : CookieSecurePolicy.Always;
    cookie.SameSite = SameSiteMode.Lax;
    cookie.Path = "/";
}

// Configure authentication paths
builder.Services.ConfigureApplicationCookie(options =>
{
    ConfigureSentinelCookie(options.Cookie, "__Host-Sentinel.Auth");
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
    options => ConfigureSentinelCookie(options.Cookie, "__Host-Sentinel.External"));
builder.Services.Configure<Microsoft.AspNetCore.Authentication.Cookies.CookieAuthenticationOptions>(
    IdentityConstants.TwoFactorRememberMeScheme,
    options => ConfigureSentinelCookie(options.Cookie, "__Host-Sentinel.TwoFactorRememberMe"));
builder.Services.Configure<Microsoft.AspNetCore.Authentication.Cookies.CookieAuthenticationOptions>(
    IdentityConstants.TwoFactorUserIdScheme,
    options => ConfigureSentinelCookie(options.Cookie, "__Host-Sentinel.TwoFactorUserId"));

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
    ConfigureSentinelCookie(options.Cookie, "__Host-Sentinel.Session");
    options.Cookie.IsEssential = true;
});

// The antiforgery token is supplied in forms/headers, so its cookie can remain
// HttpOnly. Giving it its own host-only, HTTPS-only name avoids cookie shadowing.
builder.Services.AddAntiforgery(options =>
{
    ConfigureSentinelCookie(options.Cookie, "__Host-Sentinel.AntiForgery");
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

builder.Services.AddSentinelDomainServices(builder.Configuration, builder.Environment);

var app = builder.Build();

// Fail closed at startup if the required ASVS password deny-list is missing or
// incomplete instead of silently allowing password checks to be bypassed.
_ = app.Services.GetRequiredService<ICommonPasswordDenyList>();

app.UseSentinelApplicationPipeline(useForwardedHeaders);
app.MapSentinelMinimalApis();
app.MapSentinelHealthCheck();
await app.InitializeSentinelAsync();

app.Run();


