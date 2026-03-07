using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Sentinel.Data;
using Sentinel.Models;
using Sentinel.Services;
using AntDesign;
using System.Text.Json;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using System.Threading.RateLimiting;

var builder = WebApplication.CreateBuilder(args);

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

// Configure Kestrel to allow larger request bodies (for shapefile uploads)
builder.Services.Configure<KestrelServerOptions>(options =>
{
    options.Limits.MaxRequestBodySize = 104_857_600; // 100MB
    options.Limits.RequestHeadersTimeout = TimeSpan.FromMinutes(5);
});

// Configure Form options for multipart requests
builder.Services.Configure<Microsoft.AspNetCore.Http.Features.FormOptions>(options =>
{
    options.MultipartBodyLengthLimit = 104_857_600; // 100MB
    options.ValueLengthLimit = 104_857_600;
    options.MultipartHeadersLengthLimit = 16384;
    options.BufferBodyLengthLimit = 104_857_600;
});

// Database
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

// Register the CaseCreationInterceptor (must be registered before DbContext)
builder.Services.AddSingleton<CaseCreationInterceptor>();

builder.Services.AddDbContext<ApplicationDbContext>((serviceProvider, options) =>
{
    // Get the interceptor from DI
    var caseInterceptor = serviceProvider.GetRequiredService<CaseCreationInterceptor>();
    
    options.UseSqlServer(connectionString, sqlOptions =>
    {
        // Retry on transient failures (connection issues, timeouts)
        sqlOptions.EnableRetryOnFailure(
            maxRetryCount: 5,
            maxRetryDelay: TimeSpan.FromSeconds(10),
            errorNumbersToAdd: null);
    })
    .AddInterceptors(caseInterceptor); // Auto-create tasks for new cases
});

// Identity (include roles so RoleManager and role stores are registered)
builder.Services.AddDefaultIdentity<ApplicationUser>(options =>
{
    options.SignIn.RequireConfirmedAccount = false;
    
    // Strengthen password requirements
    options.Password.RequiredLength = 12;
    options.Password.RequireDigit = true;
    options.Password.RequireLowercase = true;
    options.Password.RequireUppercase = true;
    options.Password.RequireNonAlphanumeric = true;
    options.Password.RequiredUniqueChars = 4;
    
    // Lockout on failed attempts
    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
    options.Lockout.MaxFailedAccessAttempts = 5;
    options.Lockout.AllowedForNewUsers = true;
})
.AddRoles<IdentityRole>()
.AddEntityFrameworkStores<ApplicationDbContext>();

// Configure authentication paths
builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Identity/Account/Login";
    options.LogoutPath = "/Identity/Account/Logout";
    options.AccessDeniedPath = "/Identity/Account/AccessDenied";
});

// Authorization with custom permission policy provider
builder.Services.AddAuthorization();
builder.Services.AddSingleton<Microsoft.AspNetCore.Authorization.IAuthorizationPolicyProvider, Sentinel.Authorization.PermissionPolicyProvider>();
builder.Services.AddScoped<Microsoft.AspNetCore.Authorization.IAuthorizationHandler, Sentinel.Authorization.PermissionHandler>();

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
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

// Razor Pages with global authorization
builder.Services.AddRazorPages(options =>
{
    // Require authentication for all pages by default
    options.Conventions.AuthorizeFolder("/");
    
    // Allow anonymous access to Identity pages (login, register, forgot password, etc.)
    options.Conventions.AllowAnonymousToAreaFolder("Identity", "/Account");
})
.AddSessionStateTempDataProvider(); // Use session instead of cookies for TempData

// API Controllers (for AJAX endpoints)
builder.Services.AddControllers();

// Blazor Server (for interactive settings/components)
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

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
builder.Services.AddScoped<Sentinel.Services.CustomFieldService>();
builder.Services.AddScoped<Sentinel.Services.ITaskService, Sentinel.Services.TaskService>();
builder.Services.AddScoped<Sentinel.Services.ITaskAssignmentService, Sentinel.Services.TaskAssignmentService>();
builder.Services.AddScoped<Sentinel.Services.ISurveyService, Sentinel.Services.SurveyService>();
builder.Services.AddScoped<Sentinel.Services.IOutbreakService, Sentinel.Services.OutbreakService>();
builder.Services.AddScoped<Sentinel.Services.ILineListService, Sentinel.Services.LineListService>();
builder.Services.AddScoped<Sentinel.Services.IDuplicateDetectionService, Sentinel.Services.DuplicateDetectionService>();
builder.Services.AddScoped<Sentinel.Services.IJurisdictionService, Sentinel.Services.JurisdictionService>();
builder.Services.AddScoped<Sentinel.Services.Reporting.IReportFieldMetadataService, Sentinel.Services.Reporting.ReportFieldMetadataService>();
builder.Services.AddScoped<Sentinel.Services.Reporting.IReportDataService, Sentinel.Services.Reporting.ReportDataService>();
builder.Services.AddScoped<Sentinel.Services.Reporting.IReportFolderService, Sentinel.Services.Reporting.ReportFolderService>();
builder.Services.AddScoped<Sentinel.Services.IDataReviewService, Sentinel.Services.DataReviewService>();
builder.Services.AddScoped<Sentinel.Services.ISurveyMappingService, Sentinel.Services.SurveyMappingService>();
builder.Services.AddScoped<Sentinel.Services.ICollectionMappingService, Sentinel.Services.CollectionMappingService>();
builder.Services.AddScoped<Sentinel.Services.CollectionMappingValidationService>();
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
    builder.Services.AddHttpClient<Sentinel.Services.ILocationLookupService, Sentinel.Services.GoogleLocationLookupService>(c =>
    {
        c.BaseAddress = new Uri("https://maps.googleapis.com/maps/api/");
    });
}

// Legacy Geocoding Service (delegates to ILocationLookupService for backward compatibility)
builder.Services.AddScoped<Sentinel.Services.IGeocodingService, Sentinel.Services.GoogleGeocodingService>();

var app = builder.Build();

// Middleware
if (app.Environment.IsDevelopment())
{
    app.UseMigrationsEndPoint();
}
else
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseStatusCodePagesWithReExecute("/not-found");

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseSession(); // Add session middleware (must be before authentication)

app.UseRateLimiter(); // Rate limiting middleware

app.UseAuthentication();
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
});



// API endpoint for jurisdiction autocomplete
app.MapGet("/api/jurisdictions/search", async (string term, int? typeId, ApplicationDbContext context) =>
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
});

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
});

// API endpoint to get lab results for a case
app.MapGet("/api/cases/{caseId}/lab-results", async (Guid caseId, ApplicationDbContext context) =>
{
    var labResults = await context.LabResults
        .Include(lr => lr.TestType)
        .Include(lr => lr.TestResult)
        .Include(lr => lr.SpecimenType)
        .Include(lr => lr.ResultUnits)
        .Where(lr => lr.CaseId == caseId)
        .OrderByDescending(lr => lr.SpecimenCollectionDate)
        .Select(lr => new
        {
            Id = lr.Id,
            FriendlyId = lr.FriendlyId,
            TestTypeName = lr.TestType != null ? lr.TestType.Name : null,
            TestResultName = lr.TestResult != null ? lr.TestResult.Name : null,
            SpecimenTypeName = lr.SpecimenType != null ? lr.SpecimenType.Name : null,
            SpecimenCollectionDate = lr.SpecimenCollectionDate,
            QuantitativeResult = lr.QuantitativeResult,
            ResultUnitsName = lr.ResultUnits != null ? lr.ResultUnits.Name : null
        })
        .ToListAsync();

    return Results.Json(labResults);
});

// API endpoint to delete a lab result
app.MapDelete("/api/lab-results/{id}", async (Guid id, ApplicationDbContext context) =>
{
    var labResult = await context.LabResults.FindAsync(id);
    if (labResult == null)
        return Results.NotFound();

    context.LabResults.Remove(labResult);
    await context.SaveChangesAsync();

    return Results.Ok();
});

// API endpoint to get exposures for a case
app.MapGet("/api/cases/{caseId}/exposures", async (Guid caseId, ApplicationDbContext context) =>
{
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
});

// API endpoint to delete an exposure
app.MapDelete("/api/exposures/{id}", async (Guid id, ApplicationDbContext context) =>
{
    var exposure = await context.ExposureEvents.FindAsync(id);
    if (exposure == null)
        return Results.NotFound();

    context.ExposureEvents.Remove(exposure);
    await context.SaveChangesAsync();

    return Results.Ok();
});

// API endpoint to get patient address for a case
app.MapGet("/api/patients/{caseId}/address", async (Guid caseId, ApplicationDbContext context) =>
{
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
});

// API endpoint for user autocomplete (for task assignment)
app.MapGet("/api/users/search", async (string term, Microsoft.AspNetCore.Identity.UserManager<ApplicationUser> userManager) =>
{
    if (string.IsNullOrWhiteSpace(term))
        return Results.Json(Array.Empty<object>());

    var users = userManager.Users
        .Where(u => u.Email != null && u.Email.Contains(term))
        .OrderBy(u => u.Email)
        .Take(20)
        .Select(u => new
        {
            Id = u.Id,
            Email = u.Email,
            DisplayName = u.Email
        })
        .ToList();

    return Results.Json(users);
});

// API endpoint for disease exposure requirements
app.MapGet("/api/diseases/{id:guid}/exposure-requirements", async (Guid id, IExposureRequirementService service) =>
{
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
});

// Apply migrations and seed data on startup with retry logic
using (var scope = app.Services.CreateScope())
{
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
    
    // Seed demo users and roles (only for demo environment)
    if (app.Environment.EnvironmentName.Equals("Demo", StringComparison.OrdinalIgnoreCase) || 
        app.Configuration.GetValue<bool>("Demo:EnableDemoUsers"))
    {
        logger.LogInformation("?? Demo environment detected - seeding demo users and roles...");
        await Sentinel.Services.DemoUserSeedService.SeedAsync(scope.ServiceProvider);
    }
}

// Health check endpoint for Docker and monitoring
app.MapGet("/health", async (ApplicationDbContext dbContext) =>
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
        return Results.Json(new
        {
            status = "unhealthy",
            application = "Sentinel",
            timestamp = DateTime.UtcNow,
            database = "disconnected",
            error = ex.Message
        }, statusCode: 503);
    }
}).AllowAnonymous();

app.Run();

