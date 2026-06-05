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

// Set demo mode flag from configuration (drives version display, demo UI, etc.)
Sentinel.Constants.AppVersion.IsDemoMode = builder.Configuration.GetValue<bool>("Demo:EnableDemoMode");

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
    
    // Get command timeout from configuration (default 30 seconds, increase for large bulk imports)
    var commandTimeout = builder.Configuration.GetValue<int?>("Database:CommandTimeoutSeconds") ?? 30;
    
    options.UseSqlServer(connectionString, sqlOptions =>
    {
        // Set command timeout for long-running operations (bulk imports, etc.)
        sqlOptions.CommandTimeout(commandTimeout);
        
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

// Add claims transformation to populate permission claims for Razor views
builder.Services.AddScoped<Microsoft.AspNetCore.Authentication.IClaimsTransformation, Sentinel.Authorization.PermissionClaimsTransformation>();

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
builder.Services.AddScoped<Sentinel.Services.Reporting.ICollectionMetadataService, Sentinel.Services.Reporting.CollectionMetadataService>();
builder.Services.AddScoped<Sentinel.Services.Reporting.IDynamicDateResolver, Sentinel.Services.Reporting.DynamicDateResolver>();
builder.Services.AddScoped<Sentinel.Services.IDataReviewService, Sentinel.Services.DataReviewService>();

// Case Definition Evaluation Services
builder.Services.AddScoped<Sentinel.Services.CaseDefinitionEvaluation.OperatorEvaluator>();
builder.Services.AddScoped<Sentinel.Services.CaseDefinitionEvaluation.FieldResolver>();
builder.Services.AddScoped<Sentinel.Services.CaseDefinitionEvaluation.CriterionEvaluator>();
builder.Services.AddScoped<Sentinel.Services.CaseDefinitionEvaluation.CriteriaGroupEvaluator>();
builder.Services.AddScoped<Sentinel.Services.CaseDefinitionEvaluation.DefinitionEvaluator>();
builder.Services.AddScoped<Sentinel.Services.CaseDefinitionEvaluation.ICaseDefinitionEvaluationService, Sentinel.Services.CaseDefinitionEvaluation.CaseDefinitionEvaluationService>();

// HL7 Services
builder.Services.AddScoped<Sentinel.Services.HL7.IHL7ParserService, Sentinel.Services.HL7.HL7ParserService>();
builder.Services.AddScoped<Sentinel.Services.HL7.IDuplicateDetectionService, Sentinel.Services.HL7.DuplicateDetectionService>();
builder.Services.AddScoped<Sentinel.Services.HL7.IHL7FieldMappingService, Sentinel.Services.HL7.HL7FieldMappingService>();
builder.Services.AddScoped<Sentinel.Services.HL7.IHL7DataExtractionService, Sentinel.Services.HL7.HL7DataExtractionService>();
builder.Services.AddScoped<Sentinel.Services.HL7.IHL7MarkerResolutionService, Sentinel.Services.HL7.HL7MarkerResolutionService>();
builder.Services.AddScoped<Sentinel.Services.HL7.ICaseDefinitionMatchingService, Sentinel.Services.HL7.CaseDefinitionMatchingService>();
builder.Services.AddScoped<Sentinel.Services.HL7.ICaseMatchingService, Sentinel.Services.HL7.CaseMatchingService>();
builder.Services.AddScoped<Sentinel.Services.HL7.HL7DiagnosticService>();
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

// Natural Language Timeline Entry Services
builder.Services.AddScoped<Sentinel.Services.INaturalLanguageParserService, Sentinel.Services.NaturalLanguageParserService>();
builder.Services.AddScoped<Sentinel.Services.ITimelineStorageService, Sentinel.Services.TimelineStorageService>();
builder.Services.AddScoped<Sentinel.Services.IEntityMemoryService, Sentinel.Services.EntityMemoryService>();

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
});



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
app.MapGet("/api/users/search", async (string? term, Microsoft.AspNetCore.Identity.UserManager<ApplicationUser> userManager) =>
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

// API endpoint to reorder case definition criteria
app.MapPatch("/api/case-definitions/{id:int}/criteria/{criterionId:int}/reorder", async (int id, int criterionId, ApplicationDbContext context, HttpRequest request, ILogger<Program> logger) =>
{
    using var reader = new StreamReader(request.Body);
    var body = await reader.ReadToEndAsync();
    var data = JsonSerializer.Deserialize<JsonElement>(body);

    if (!data.TryGetProperty("direction", out var directionElement))
        return Results.BadRequest("Direction is required");

    var direction = directionElement.GetString();
    if (direction != "up" && direction != "down")
        return Results.BadRequest("Direction must be 'up' or 'down'");

    logger.LogInformation("Reordering criterion {CriterionId} in definition {DefinitionId}, direction: {Direction}", criterionId, id, direction);

    var criterion = await context.CaseDefinitionCriteria
        .FirstOrDefaultAsync(c => c.Id == criterionId && c.CaseDefinitionId == id);

    if (criterion == null)
    {
        logger.LogWarning("Criterion {CriterionId} not found", criterionId);
        return Results.NotFound();
    }

    // Get all criteria at the same level (same parent and group)
    var siblings = await context.CaseDefinitionCriteria
        .Where(c => c.CaseDefinitionId == id && 
                    c.ParentCriteriaId == criterion.ParentCriteriaId &&
                    c.GroupNumber == criterion.GroupNumber)
        .OrderBy(c => c.DisplayOrder)
        .ThenBy(c => c.Id) // Secondary sort by ID for stability when DisplayOrders are equal
        .ToListAsync();

    logger.LogInformation("Found {Count} siblings. Current criterion DisplayOrder: {DisplayOrder}", siblings.Count, criterion.DisplayOrder);

    var currentIndex = siblings.FindIndex(c => c.Id == criterionId);
    if (currentIndex == -1)
        return Results.NotFound();

    // Calculate new index
    var newIndex = direction == "up" ? currentIndex - 1 : currentIndex + 1;

    // Check bounds
    if (newIndex < 0 || newIndex >= siblings.Count)
    {
        logger.LogWarning("Cannot move beyond bounds. Current index: {CurrentIndex}, New index: {NewIndex}, Total: {Total}", currentIndex, newIndex, siblings.Count);
        return Results.BadRequest("Cannot move criterion beyond bounds");
    }

    // Remove from current position and insert at new position
    var item = siblings[currentIndex];
    siblings.RemoveAt(currentIndex);
    siblings.Insert(newIndex, item);

    // Re-index all siblings with sequential DisplayOrder values
    for (int i = 0; i < siblings.Count; i++)
    {
        siblings[i].DisplayOrder = i;
        logger.LogInformation("Updated criterion {Id} DisplayOrder to {Order}", siblings[i].Id, i);
    }

    await context.SaveChangesAsync();

    logger.LogInformation("Changes saved successfully");

    return Results.Ok();
});

// API endpoint to move criteria to a different parent
app.MapPatch("/api/case-definitions/{id:int}/criteria/{criterionId:int}/move-to-parent", async (int id, int criterionId, ApplicationDbContext context, HttpRequest request) =>
{
    using var reader = new StreamReader(request.Body);
    var body = await reader.ReadToEndAsync();
    var data = JsonSerializer.Deserialize<JsonElement>(body);

    int? parentCriteriaId = null;
    if (data.TryGetProperty("parentCriteriaId", out var parentElement) && 
        parentElement.ValueKind != JsonValueKind.Null)
    {
        parentCriteriaId = parentElement.GetInt32();
    }

    var criterion = await context.CaseDefinitionCriteria
        .FirstOrDefaultAsync(c => c.Id == criterionId && c.CaseDefinitionId == id);

    if (criterion == null)
        return Results.NotFound();

    // Validate parent exists if specified
    if (parentCriteriaId.HasValue)
    {
        var parentExists = await context.CaseDefinitionCriteria
            .AnyAsync(c => c.Id == parentCriteriaId.Value && c.CaseDefinitionId == id);

        if (!parentExists)
            return Results.BadRequest("Parent criterion not found");
    }

    criterion.ParentCriteriaId = parentCriteriaId;
    await context.SaveChangesAsync();

    return Results.Ok();
});

// API endpoint to get a single criterion by ID
app.MapGet("/api/case-definitions/{id:int}/criteria/{criterionId:int}", async (int id, int criterionId, ApplicationDbContext context) =>
{
    var criterion = await context.CaseDefinitionCriteria
        .FirstOrDefaultAsync(c => c.Id == criterionId && c.CaseDefinitionId == id);

    if (criterion == null)
        return Results.NotFound();

    return Results.Json(new
    {
        id = criterion.Id,
        caseDefinitionId = criterion.CaseDefinitionId,
        parentCriteriaId = criterion.ParentCriteriaId,
        criterionType = (int)criterion.CriterionType,
        logicalOperator = (int)criterion.LogicalOperator,
        groupNumber = criterion.GroupNumber,
        fieldPath = criterion.FieldPath,
        @operator = (int)criterion.Operator,
        valueJson = criterion.ValueJson,
        displayText = criterion.DisplayText,
        displayOrder = criterion.DisplayOrder
    });
});

// API endpoint to update laboratory criterion
app.MapPut("/api/case-definitions/{id:int}/criteria/{criterionId:int}/laboratory", async (int id, int criterionId, ApplicationDbContext context, HttpRequest request) =>
{
    using var reader = new StreamReader(request.Body);
    var body = await reader.ReadToEndAsync();
    var data = JsonSerializer.Deserialize<JsonElement>(body);

    var criterion = await context.CaseDefinitionCriteria
        .FirstOrDefaultAsync(c => c.Id == criterionId && c.CaseDefinitionId == id);

    if (criterion == null)
        return Results.NotFound();

    // Extract data
    var specimenTypeIds = data.GetProperty("specimenTypeIds").EnumerateArray()
        .Select(e => e.GetInt32()).ToList();
    var pathogenNames = data.GetProperty("pathogenNames").EnumerateArray()
        .Select(e => e.GetString()!).ToList();
    var testMethodIds = data.GetProperty("testMethodIds").EnumerateArray()
        .Select(e => e.GetInt32()).ToList();
    var resultValues = data.GetProperty("resultValues").EnumerateArray()
        .Select(e => e.GetString()!).ToList();

    object? timeConstraint = null;
    if (data.TryGetProperty("timeConstraint", out var timeConstraintElement) && 
        timeConstraintElement.ValueKind != JsonValueKind.Null)
    {
        timeConstraint = new
        {
            days = timeConstraintElement.GetProperty("days").GetInt32(),
            relativeTo = timeConstraintElement.GetProperty("relativeTo").GetString(),
            direction = timeConstraintElement.GetProperty("direction").GetString()
        };
    }

    // Extract storage preferences
    int? specimenStoragePreference = data.TryGetProperty("specimenStoragePreference", out var ssp) ? ssp.GetInt32() : (int?)null;
    int? canonicalSpecimenTypeId = data.TryGetProperty("canonicalSpecimenTypeId", out var cst) && cst.ValueKind != JsonValueKind.Null ? cst.GetInt32() : (int?)null;
    int? pathogenStoragePreference = data.TryGetProperty("pathogenStoragePreference", out var psp) ? psp.GetInt32() : (int?)null;
    string? canonicalPathogenId = data.TryGetProperty("canonicalPathogenId", out var cpg) && cpg.ValueKind != JsonValueKind.Null ? cpg.GetString() : null;
    int? testMethodStoragePreference = data.TryGetProperty("testMethodStoragePreference", out var tsp) ? tsp.GetInt32() : (int?)null;
    int? canonicalTestMethodId = data.TryGetProperty("canonicalTestMethodId", out var ctm) && ctm.ValueKind != JsonValueKind.Null ? ctm.GetInt32() : (int?)null;
    int? resultStoragePreference = data.TryGetProperty("resultStoragePreference", out var rsp) ? rsp.GetInt32() : (int?)null;
    string? canonicalResultValue = data.TryGetProperty("canonicalResultValue", out var crv) && crv.ValueKind != JsonValueKind.Null ? crv.GetString() : null;

    // Build ValueJson including storage preferences
    var valueObj = new
    {
        specimenTypeIds,
        pathogenNames,
        testMethodIds,
        resultValues,
        timeConstraint,
        specimenStoragePreference,
        canonicalSpecimenTypeId,
        pathogenStoragePreference,
        canonicalPathogenId,
        testMethodStoragePreference,
        canonicalTestMethodId,
        resultStoragePreference,
        canonicalResultValue
    };

    // Update criterion
    criterion.ValueJson = JsonSerializer.Serialize(valueObj);
    criterion.DisplayText = data.GetProperty("displayText").GetString()!;
    criterion.LogicalOperator = (Sentinel.Models.CaseDefinitions.LogicalOperator)data.GetProperty("logicalOperator").GetInt32();

    // Update lab-specific fields directly on the criterion
    criterion.AcceptableSpecimenTypesJson = JsonSerializer.Serialize(specimenTypeIds);
    criterion.SpecimenStoragePreference = specimenStoragePreference.HasValue ? (Sentinel.Models.CaseDefinitions.DataStoragePreference)specimenStoragePreference.Value : Sentinel.Models.CaseDefinitions.DataStoragePreference.StoreAsReceived;
    criterion.CanonicalSpecimenTypeId = canonicalSpecimenTypeId;
    criterion.AcceptablePathogensJson = JsonSerializer.Serialize(pathogenNames);
    criterion.BiomarkerStoragePreference = pathogenStoragePreference.HasValue ? (Sentinel.Models.CaseDefinitions.DataStoragePreference)pathogenStoragePreference.Value : Sentinel.Models.CaseDefinitions.DataStoragePreference.StoreAsReceived;
    criterion.CanonicalPathogenId = canonicalPathogenId != null ? Guid.Parse(canonicalPathogenId) : (Guid?)null;
    criterion.AcceptableTestMethodsJson = JsonSerializer.Serialize(testMethodIds);
    criterion.TestMethodStoragePreference = testMethodStoragePreference.HasValue ? (Sentinel.Models.CaseDefinitions.DataStoragePreference)testMethodStoragePreference.Value : Sentinel.Models.CaseDefinitions.DataStoragePreference.StoreAsReceived;
    criterion.CanonicalTestMethodId = canonicalTestMethodId;
    criterion.AcceptableResultsJson = JsonSerializer.Serialize(resultValues);
    criterion.ResultStoragePreference = resultStoragePreference.HasValue ? (Sentinel.Models.CaseDefinitions.DataStoragePreference)resultStoragePreference.Value : Sentinel.Models.CaseDefinitions.DataStoragePreference.StoreAsReceived;
    criterion.Description = canonicalResultValue;

    await context.SaveChangesAsync();

    return Results.Ok(new { success = true, criterionId = criterion.Id });
});

// API endpoint to update clinical criterion
app.MapPut("/api/case-definitions/{id:int}/criteria/{criterionId:int}/clinical", async (int id, int criterionId, ApplicationDbContext context, HttpRequest request) =>
{
    using var reader = new StreamReader(request.Body);
    var body = await reader.ReadToEndAsync();
    var data = JsonSerializer.Deserialize<JsonElement>(body);

    var criterion = await context.CaseDefinitionCriteria
        .FirstOrDefaultAsync(c => c.Id == criterionId && c.CaseDefinitionId == id);

    if (criterion == null)
        return Results.NotFound();

    // Extract data
    var symptomIds = data.GetProperty("symptomIds").EnumerateArray()
        .Select(e => e.GetInt32()).ToList();
    var requireAll = data.GetProperty("requireAll").GetBoolean();

    int? minCount = null;
    if (data.TryGetProperty("minCount", out var minCountElement) && 
        minCountElement.ValueKind != JsonValueKind.Null)
    {
        minCount = minCountElement.GetInt32();
    }

    string? severityFilter = null;
    if (data.TryGetProperty("severityFilter", out var severityElement) && 
        severityElement.ValueKind != JsonValueKind.Null)
    {
        severityFilter = severityElement.GetString();
    }

    // Build ValueJson
    var valueObj = new
    {
        symptomIds,
        requireAll,
        minCount,
        severityFilter
    };

    // Update criterion
    criterion.ValueJson = JsonSerializer.Serialize(valueObj);
    criterion.DisplayText = data.GetProperty("displayText").GetString()!;
    criterion.LogicalOperator = (Sentinel.Models.CaseDefinitions.LogicalOperator)data.GetProperty("logicalOperator").GetInt32();
    criterion.Operator = requireAll ? Sentinel.Models.CaseDefinitions.ComparisonOperator.Equals : Sentinel.Models.CaseDefinitions.ComparisonOperator.InList;

    await context.SaveChangesAsync();

    return Results.Ok(new { success = true, criterionId = criterion.Id });
});

// API endpoint to update custom field criterion
app.MapPut("/api/case-definitions/{id:int}/criteria/{criterionId:int}/custom-field", async (int id, int criterionId, ApplicationDbContext context, HttpRequest request) =>
{
    using var reader = new StreamReader(request.Body);
    var body = await reader.ReadToEndAsync();
    var data = JsonSerializer.Deserialize<JsonElement>(body);

    var criterion = await context.CaseDefinitionCriteria
        .FirstOrDefaultAsync(c => c.Id == criterionId && c.CaseDefinitionId == id);

    if (criterion == null)
        return Results.NotFound();

    // Extract data
    var customFieldId = data.GetProperty("customFieldId").GetInt32();
    var operatorValue = data.GetProperty("operator").GetString()!;
    var value = data.GetProperty("value").GetString()!;

    // Load custom field to get details
    var customField = await context.CustomFieldDefinitions
        .FirstOrDefaultAsync(cf => cf.Id == customFieldId);

    if (customField == null)
        return Results.BadRequest("Custom field not found");

    // Build ValueJson
    var valueObj = new
    {
        customFieldId,
        customFieldName = customField.Name,
        customFieldLabel = customField.Label,
        fieldType = customField.FieldType.ToString(),
        value,
        @operator = operatorValue
    };

    // Update criterion
    criterion.ValueJson = JsonSerializer.Serialize(valueObj);
    criterion.DisplayText = data.GetProperty("displayText").GetString()!;
    criterion.LogicalOperator = (Sentinel.Models.CaseDefinitions.LogicalOperator)data.GetProperty("logicalOperator").GetInt32();

    await context.SaveChangesAsync();

    return Results.Ok(new { success = true, criterionId = criterion.Id });
});

// API endpoint to update case field criterion
app.MapPut("/api/case-definitions/{id:int}/criteria/{criterionId:int}/case-field", async (int id, int criterionId, ApplicationDbContext context, HttpRequest request) =>
{
    using var reader = new StreamReader(request.Body);
    var body = await reader.ReadToEndAsync();
    var data = JsonSerializer.Deserialize<JsonElement>(body);

    var criterion = await context.CaseDefinitionCriteria
        .FirstOrDefaultAsync(c => c.Id == criterionId && c.CaseDefinitionId == id);

    if (criterion == null)
        return Results.NotFound();

    // Extract data
    var fieldPath = data.GetProperty("fieldPath").GetString()!;
    var operatorValue = data.GetProperty("operator").GetString()!;
    var value = data.GetProperty("value").GetString()!;

    // Build ValueJson
    var valueObj = new
    {
        fieldPath,
        @operator = operatorValue,
        value
    };

    // Update criterion
    criterion.FieldPath = fieldPath;
    criterion.ValueJson = JsonSerializer.Serialize(valueObj);
    criterion.DisplayText = data.GetProperty("displayText").GetString()!;
    criterion.LogicalOperator = (Sentinel.Models.CaseDefinitions.LogicalOperator)data.GetProperty("logicalOperator").GetInt32();

    await context.SaveChangesAsync();

    return Results.Ok(new { success = true, criterionId = criterion.Id });
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

// Helper method to ensure reporting views exist and are correct
static async Task EnsureReportingViewsExistAsync(ApplicationDbContext dbContext, ILogger logger)
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

app.Run();

