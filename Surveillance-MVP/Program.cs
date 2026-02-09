using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Surveillance_MVP.Data;
using Surveillance_MVP.Models;
using Surveillance_MVP.Services;
using AntDesign;
using System.Text.Json;

var builder = WebApplication.CreateBuilder(args);

// Database
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString));

// Identity (include roles so RoleManager and role stores are registered)
builder.Services.AddDefaultIdentity<ApplicationUser>(options =>
{
    options.SignIn.RequireConfirmedAccount = false;
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
builder.Services.AddSingleton<Microsoft.AspNetCore.Authorization.IAuthorizationPolicyProvider, Surveillance_MVP.Authorization.PermissionPolicyProvider>();
builder.Services.AddScoped<Microsoft.AspNetCore.Authorization.IAuthorizationHandler, Surveillance_MVP.Authorization.PermissionHandler>();

// Razor Pages with global authorization
builder.Services.AddRazorPages(options =>
{
    // Require authentication for all pages by default
    options.Conventions.AuthorizeFolder("/");
    
    // Allow anonymous access to Identity pages (login, register, forgot password, etc.)
    options.Conventions.AllowAnonymousToAreaFolder("Identity", "/Account");
});

// API Controllers (for AJAX endpoints)
builder.Services.AddControllers();

// Blazor Server (for interactive settings/components)
builder.Services.AddServerSideBlazor();

// app services
builder.Services.AddScoped<Surveillance_MVP.Services.IPatientDuplicateCheckService, Surveillance_MVP.Services.PatientDuplicateCheckService>();
builder.Services.AddScoped<Surveillance_MVP.Services.ILocationDuplicateCheckService, Surveillance_MVP.Services.LocationDuplicateCheckService>();
builder.Services.AddScoped<Surveillance_MVP.Services.IExposureRequirementService, Surveillance_MVP.Services.ExposureRequirementService>();
builder.Services.AddScoped<Surveillance_MVP.Services.IOccupationImportService, Surveillance_MVP.Services.OccupationImportService>();
builder.Services.AddScoped<Surveillance_MVP.Services.IAuditService, Surveillance_MVP.Services.AuditService>();
builder.Services.AddScoped<Surveillance_MVP.Services.IPatientCustomFieldService, Surveillance_MVP.Services.PatientCustomFieldService>();
builder.Services.AddScoped<Surveillance_MVP.Services.IPatientMergeService, Surveillance_MVP.Services.PatientMergeService>();
builder.Services.AddScoped<Surveillance_MVP.Services.IPatientIdGeneratorService, Surveillance_MVP.Services.PatientIdGeneratorService>();
builder.Services.AddScoped<Surveillance_MVP.Services.ICaseIdGeneratorService, Surveillance_MVP.Services.CaseIdGeneratorService>();
builder.Services.AddScoped<Surveillance_MVP.Services.IPermissionService, Surveillance_MVP.Services.PermissionService>();
builder.Services.AddScoped<Surveillance_MVP.Services.IDiseaseAccessService, Surveillance_MVP.Services.DiseaseAccessService>();
builder.Services.AddScoped<Surveillance_MVP.Services.CustomFieldService>();
builder.Services.AddScoped<Surveillance_MVP.Services.ITaskService, Surveillance_MVP.Services.TaskService>();
builder.Services.AddScoped<Surveillance_MVP.Services.ITaskAssignmentService, Surveillance_MVP.Services.TaskAssignmentService>();
builder.Services.AddScoped<Surveillance_MVP.Services.ISurveyService, Surveillance_MVP.Services.SurveyService>();
builder.Services.AddScoped<Surveillance_MVP.Services.IOutbreakService, Surveillance_MVP.Services.OutbreakService>();
builder.Services.AddScoped<Surveillance_MVP.Services.ILineListService, Surveillance_MVP.Services.LineListService>();
builder.Services.AddScoped<Surveillance_MVP.Helpers.PermissionHelper>();

// HttpContextAccessor for audit logging
builder.Services.AddHttpContextAccessor();

// AntDesign
builder.Services.AddAntDesign();

// Geocoding clients - use Google Maps APIs (Places Autocomplete + Geocoding)
builder.Services.AddHttpClient<Surveillance_MVP.Services.IGeocodingService, Surveillance_MVP.Services.GoogleGeocodingService>(c =>
{
    c.BaseAddress = new Uri("https://maps.googleapis.com/maps/api/");
});

// Named client used by the suggestions API
builder.Services.AddHttpClient("google", c =>
{
    c.BaseAddress = new Uri("https://maps.googleapis.com/maps/api/");
});

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

app.UseAuthentication();
app.UseAuthorization();

// Razor Pages routing
app.MapRazorPages();

// API Controllers routing
app.MapControllers();

// Blazor hub for Server-side components
app.MapBlazorHub();

// Minimal API endpoint for address suggestions (returns display, lat, lon and address components)
// Minimal API endpoint for address suggestions (returns display, lat, lon and address components)
app.MapGet("/api/address-suggest", async (HttpRequest req, IHttpClientFactory factory) =>
{
    var q = req.Query["q"].ToString();
    var limitStr = req.Query["limit"].ToString();
    if (string.IsNullOrWhiteSpace(q))
        return Results.Json(Array.Empty<object>());

    var apiKey = app.Configuration["Geocoding:ApiKey"] ?? string.Empty;
    if (string.IsNullOrWhiteSpace(apiKey))
        return Results.Json(Array.Empty<object>());

    var client = factory.CreateClient("google");
    var limit = 5;
    if (!string.IsNullOrWhiteSpace(limitStr) && int.TryParse(limitStr, out var parsed)) limit = parsed;

    // Get organization country code for biasing results
    var countryCode = app.Configuration["Organization:CountryCode"];

    // 1) Place Autocomplete to get place_ids
    var autoUrl = $"place/autocomplete/json?input={Uri.EscapeDataString(q)}&types=address&key={Uri.EscapeDataString(apiKey)}";
    
    // Add country code bias if configured
    if (!string.IsNullOrWhiteSpace(countryCode))
    {
        autoUrl += $"&components=country:{Uri.EscapeDataString(countryCode)}";
    }
    
    var autoResp = await client.GetAsync(autoUrl);
    if (!autoResp.IsSuccessStatusCode)
        return Results.Json(Array.Empty<object>());

    var autoBody = await autoResp.Content.ReadAsStringAsync();
    using var autoDoc = JsonDocument.Parse(autoBody);
    var preds = autoDoc.RootElement.TryGetProperty("predictions", out var predEl) ? predEl : default;

    var list = new List<object>();
    if (preds.ValueKind == JsonValueKind.Array)
    {
        var i = 0;
        foreach (var p in preds.EnumerateArray())
        {
            if (i++ >= limit) break;
            var placeId = p.GetProperty("place_id").GetString();
            if (string.IsNullOrWhiteSpace(placeId)) continue;

            // 2) Place Details to get geometry and address components
            var detailsUrl = $"place/details/json?place_id={Uri.EscapeDataString(placeId)}&fields=formatted_address,geometry,address_component&key={Uri.EscapeDataString(apiKey)}";
            var detResp = await client.GetAsync(detailsUrl);
            if (!detResp.IsSuccessStatusCode) continue;

            var detBody = await detResp.Content.ReadAsStringAsync();
            using var detDoc = JsonDocument.Parse(detBody);
            var root = detDoc.RootElement;
            if (!root.TryGetProperty("result", out var res)) continue;

            var formatted = res.TryGetProperty("formatted_address", out var fa) ? fa.GetString() : null;
            double? lat = null, lon = null;
            if (res.TryGetProperty("geometry", out var geom) && geom.TryGetProperty("location", out var loc))
            {
                if (loc.TryGetProperty("lat", out var latEl) && latEl.ValueKind == JsonValueKind.Number)
                    lat = latEl.GetDouble();
                if (loc.TryGetProperty("lng", out var lonEl) && lonEl.ValueKind == JsonValueKind.Number)
                    lon = lonEl.GetDouble();
            }

            var addressDict = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
            if (res.TryGetProperty("address_components", out var ac) && ac.ValueKind == JsonValueKind.Array)
            {
                foreach (var comp in ac.EnumerateArray())
                {
                    var longName = comp.TryGetProperty("long_name", out var ln) ? ln.GetString() : null;
                    if (comp.TryGetProperty("types", out var typesEl) && typesEl.ValueKind == JsonValueKind.Array)
                    {
                        foreach (var t in typesEl.EnumerateArray())
                        {
                            var type = t.GetString();
                            if (string.IsNullOrWhiteSpace(type)) continue;
                            // prefer the first value for a type
                            if (!addressDict.ContainsKey(type)) addressDict[type] = longName;
                        }
                    }
                }
            }

            list.Add(new { display = formatted, lat, lon, address = addressDict });
        }
    }

    return Results.Json(list);
});

// API endpoint for country autocomplete
app.MapGet("/api/countries/search", async (string term, ApplicationDbContext context) =>
{
    if (string.IsNullOrWhiteSpace(term))
        return Results.Json(Array.Empty<object>());

    var countries = await context.Countries
        .Where(c => c.IsActive && c.Name.Contains(term))
        .OrderBy(c => c.Name)
        .Take(20)
        .Select(c => new { c.Code, c.Name })
        .ToListAsync();

    return Results.Json(countries);
});

// API endpoint for case autocomplete with details
app.MapGet("/api/cases/search", async (string term, Guid? excludeCaseId, Guid? diseaseId, ApplicationDbContext context) =>
{
    if (string.IsNullOrWhiteSpace(term))
        return Results.Json(Array.Empty<object>());

    var query = context.Cases
        .Include(c => c.Patient)
        .Include(c => c.Disease)
            .ThenInclude(d => d.ParentDisease)
        .AsQueryable();

    if (excludeCaseId.HasValue)
    {
        query = query.Where(c => c.Id != excludeCaseId.Value);
    }

    // Filter by disease hierarchy if diseaseId is provided
    if (diseaseId.HasValue)
    {
        // Get all diseases that share the same top-level parent
        var allDiseases = await context.Diseases
            .Include(d => d.ParentDisease)
            .Where(d => d.IsActive)
            .ToListAsync();

        var targetDisease = allDiseases.FirstOrDefault(d => d.Id == diseaseId.Value);
        if (targetDisease != null)
        {
            var topLevelId = GetTopLevelId(targetDisease, allDiseases);
            
            var matchingDiseaseIds = allDiseases
                .Where(d => GetTopLevelId(d, allDiseases) == topLevelId)
                .Select(d => d.Id)
                .ToList();

            query = query.Where(c => c.DiseaseId.HasValue && matchingDiseaseIds.Contains(c.DiseaseId.Value));
        }
    }

    query = query.Where(c => 
        c.FriendlyId.Contains(term) ||
        (c.Patient != null && (c.Patient.GivenName.Contains(term) || c.Patient.FamilyName.Contains(term))));

    var cases = await query
        .OrderByDescending(c => c.DateOfNotification ?? c.DateOfOnset ?? DateTime.MinValue)
        .Take(20)
        .Select(c => new
        {
            Id = c.Id,
            FriendlyId = c.FriendlyId,
            PatientName = c.Patient != null ? c.Patient.GivenName + " " + c.Patient.FamilyName : "Unknown",
            NotificationDate = c.DateOfNotification.HasValue ? c.DateOfNotification.Value.ToString("dd MMM yyyy") : null,
            Disease = c.Disease != null ? c.Disease.Name : null
        })
        .ToListAsync();

    return Results.Json(cases);

    // Helper function to get top-level disease ID
    Guid GetTopLevelId(Surveillance_MVP.Models.Lookups.Disease disease, List<Surveillance_MVP.Models.Lookups.Disease> allDiseases)
    {
        var current = disease;
        while (current.ParentDiseaseId.HasValue)
        {
            var parent = allDiseases.FirstOrDefault(d => d.Id == current.ParentDiseaseId.Value);
            if (parent == null)
                break;
            current = parent;
        }
        return current.Id;
    }
});

// API endpoint for event autocomplete
app.MapGet("/api/events/search", async (string term, ApplicationDbContext context) =>
{
    if (string.IsNullOrWhiteSpace(term))
        return Results.Json(Array.Empty<object>());

    var events = await context.Events
        .Include(e => e.Location)
        .Include(e => e.EventType)
        .Where(e => e.IsActive && e.Name.Contains(term))
        .OrderByDescending(e => e.StartDateTime)
        .Take(20)
        .Select(e => new
        {
            Id = e.Id,
            Name = e.Name,
            StartDate = e.StartDateTime.ToString("dd MMM yyyy"),
            Location = e.Location != null ? e.Location.Name : null,
            EventType = e.EventType != null ? e.EventType.Name : null
        })
        .ToListAsync();

    return Results.Json(events);
});

// API endpoint for location autocomplete
app.MapGet("/api/locations/search", async (string term, ApplicationDbContext context) =>
{
    if (string.IsNullOrWhiteSpace(term))
        return Results.Json(Array.Empty<object>());

    var locations = await context.Locations
        .Include(l => l.LocationType)
        .Where(l => l.IsActive && l.Name.Contains(term))
        .OrderBy(l => l.Name)
        .Take(20)
        .Select(l => new
        {
            Id = l.Id,
            Name = l.Name,
            Address = l.Address,
            LocationType = l.LocationType != null ? l.LocationType.Name : null
        })
        .ToListAsync();

    return Results.Json(locations);
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

// API endpoint for disease autocomplete
app.MapGet("/api/diseases/search", async (string term, ApplicationDbContext context) =>
{
    if (string.IsNullOrWhiteSpace(term))
        return Results.Json(Array.Empty<object>());

    var diseases = await context.Diseases
        .Where(d => d.IsActive && d.Name.Contains(term))
        .OrderBy(d => d.Level)
        .ThenBy(d => d.DisplayOrder)
        .ThenBy(d => d.Name)
        .Take(20)
        .Select(d => new
        {
            Id = d.Id,
            Name = d.Name,
            Code = d.Code,
            Level = d.Level,
            ParentDiseaseId = d.ParentDiseaseId
        })
        .ToListAsync();

    return Results.Json(diseases);
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
        isRequired = disease?.ExposureTrackingMode == Surveillance_MVP.Models.ExposureTrackingMode.LocalSpecificRegion ||
                     disease?.ExposureTrackingMode == Surveillance_MVP.Models.ExposureTrackingMode.OverseasAcquired,
        defaultToResidential = disease?.DefaultToResidentialAddress ?? false,
        requireCoordinates = disease?.RequireGeographicCoordinates ?? false,
        allowDomestic = disease?.AllowDomesticAcquisition ?? true
    });
});

// Seed permissions on startup
using (var scope = app.Services.CreateScope())
{
    await Surveillance_MVP.Services.PermissionSeedService.SeedAsync(scope.ServiceProvider);
}

app.Run();

