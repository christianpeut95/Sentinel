using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Sentinel.Data;
using Sentinel.Models;
using Sentinel.Services;

namespace Sentinel.Extensions;

/// <summary>
/// Maps Sentinel's small browser-support APIs. Keeping them together makes
/// their authorization and object-level access requirements easier to audit.
/// </summary>
public static class SentinelMinimalApiExtensions
{
    public static WebApplication MapSentinelMinimalApis(this WebApplication app)
    {
        app.MapGet("/api/address-suggest", async (HttpRequest req, ILocationLookupService locationService) =>
        {
            var query = req.Query["q"].ToString();
            if (string.IsNullOrWhiteSpace(query))
                return Results.Json(Array.Empty<object>());

            var limit = TryReadLimit(req.Query["limit"], 5);
            var results = await locationService.SearchAddressesAsync(query, limit);
            return Results.Json(results.Select(result => new
            {
                display = result.Display,
                lat = result.Latitude,
                lon = result.Longitude,
                address = result.AddressComponents
            }));
        }).RequireAuthorization("Permission.ReferenceData.View");

        app.MapGet("/api/places-suggest", async (HttpRequest req, ILocationLookupService locationService) =>
        {
            var query = req.Query["q"].ToString();
            if (string.IsNullOrWhiteSpace(query))
                return Results.Json(Array.Empty<object>());

            var limit = TryReadLimit(req.Query["limit"], 5);
            var latitude = double.TryParse(req.Query["lat"], out var parsedLatitude) ? parsedLatitude : (double?)null;
            var longitude = double.TryParse(req.Query["lon"], out var parsedLongitude) ? parsedLongitude : (double?)null;
            var results = await locationService.SearchPlacesAsync(query, limit, latitude, longitude);

            return Results.Json(results.Select(result => new
            {
                placeId = result.PlaceId,
                displayName = result.Name,
                description = result.Name,
                formattedAddress = result.Address,
                coordinates = new { lat = result.Latitude, lon = result.Longitude }
            }));
        }).RequireAuthorization("Permission.ReferenceData.View");

        app.MapGet("/api/jurisdictions/search", async (string? term, int? typeId, ApplicationDbContext context) =>
        {
            if (string.IsNullOrWhiteSpace(term) && !typeId.HasValue)
                return Results.Json(Array.Empty<object>());

            var query = context.Jurisdictions.Where(jurisdiction => jurisdiction.IsActive);
            if (typeId.HasValue)
                query = query.Where(jurisdiction => jurisdiction.JurisdictionTypeId == typeId.Value);
            if (!string.IsNullOrWhiteSpace(term))
                query = query.Where(jurisdiction => jurisdiction.Name.Contains(term) ||
                    (jurisdiction.Code != null && jurisdiction.Code.Contains(term)));

            var jurisdictions = await query
                .OrderBy(jurisdiction => jurisdiction.DisplayOrder)
                .ThenBy(jurisdiction => jurisdiction.Name)
                .Take(50)
                .Select(jurisdiction => new
                {
                    jurisdiction.Id,
                    jurisdiction.Name,
                    jurisdiction.Code,
                    jurisdiction.JurisdictionTypeId
                })
                .ToListAsync();

            return Results.Json(jurisdictions);
        }).RequireAuthorization("Permission.ReferenceData.View");

        app.MapGet("/api/organizations/search", async (string term, ApplicationDbContext context) =>
        {
            if (string.IsNullOrWhiteSpace(term))
                return Results.Json(Array.Empty<object>());

            var organizations = await context.Organizations
                .Where(organization => organization.IsActive && organization.Name.Contains(term))
                .OrderBy(organization => organization.Name)
                .Take(20)
                .Select(organization => new
                {
                    organization.Id,
                    organization.Name,
                    organization.ContactPerson,
                    organization.Phone
                })
                .ToListAsync();

            return Results.Json(organizations);
        }).RequireAuthorization("Permission.Organization.View");

        app.MapGet("/api/cases/{caseId}/lab-results", async (Guid caseId, ApplicationDbContext context, ICaseAccessService caseAccessService) =>
        {
            if (!await caseAccessService.CanAccessCaseAsync(caseId))
                return Results.NotFound();

            var labResults = await context.LabResults
                .Include(result => result.SpecimenType)
                .Include(result => result.ResultUnits)
                .Include(result => result.TestedDisease)
                .Include(result => result.Markers).ThenInclude(marker => marker.Pathogen)
                .Include(result => result.Markers).ThenInclude(marker => marker.TestMethod)
                .Where(result => result.CaseId == caseId)
                .OrderByDescending(result => result.SpecimenCollectionDate)
                .Select(result => new
                {
                    result.Id,
                    result.FriendlyId,
                    TestedDiseaseName = result.TestedDisease != null ? result.TestedDisease.Name : null,
                    SpecimenTypeName = result.SpecimenType != null ? result.SpecimenType.Name : null,
                    result.SpecimenCollectionDate,
                    ResultUnitsName = result.ResultUnits != null ? result.ResultUnits.Name : null,
                    Markers = result.Markers.Select(marker => new
                    {
                        PathogenName = marker.Pathogen != null ? marker.Pathogen.Name : null,
                        TestMethodName = marker.TestMethod != null ? marker.TestMethod.Name : null,
                        QualitativeResult = marker.QualitativeResultText,
                        marker.QuantitativeValue,
                        marker.QuantitativeUnit,
                        marker.InterpretationFlag
                    }).ToList()
                })
                .ToListAsync();

            return Results.Json(labResults);
        }).RequireAuthorization("Permission.Case.View");

        app.MapDelete("/api/lab-results/{id}", async (Guid id, ApplicationDbContext context, ICaseAccessService caseAccessService) =>
        {
            var labResult = await context.LabResults.FirstOrDefaultAsync(result => result.Id == id);
            if (labResult?.CaseId is not Guid caseId || !await caseAccessService.CanAccessCaseAsync(caseId))
                return Results.NotFound();

            context.LabResults.Remove(labResult);
            await context.SaveChangesAsync();
            return Results.Ok();
        }).RequireAuthorization("Permission.Laboratory.Delete")
          .RequireAuthorization("Permission.Case.Edit")
          .WithMetadata(new RequireAntiforgeryTokenAttribute(true))
          .RequireValidAntiforgery();

        app.MapGet("/api/cases/{caseId}/exposures", async (Guid caseId, ApplicationDbContext context, ICaseAccessService caseAccessService) =>
        {
            if (!await caseAccessService.CanAccessCaseAsync(caseId))
                return Results.NotFound();

            var exposures = await context.ExposureEvents
                .Include(exposure => exposure.Location)
                .Include(exposure => exposure.Event)
                .Where(exposure => exposure.ExposedCaseId == caseId)
                .OrderByDescending(exposure => exposure.ExposureStartDate)
                .Select(exposure => new
                {
                    exposure.Id,
                    LocationName = exposure.Location != null ? exposure.Location.Name : null,
                    EventName = exposure.Event != null ? exposure.Event.Name : null,
                    exposure.ExposureStartDate,
                    exposure.ExposureEndDate,
                    ExposureType = exposure.ExposureType.ToString(),
                    exposure.Description
                })
                .ToListAsync();

            return Results.Json(exposures);
        }).RequireAuthorization("Permission.Exposure.View");

        app.MapDelete("/api/exposures/{id}", async (Guid id, ApplicationDbContext context, ICaseAccessService caseAccessService) =>
        {
            var exposure = await context.ExposureEvents.FirstOrDefaultAsync(item => item.Id == id);
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

        app.MapGet("/api/patients/{caseId}/address", async (Guid caseId, ApplicationDbContext context, ICaseAccessService caseAccessService) =>
        {
            if (!await caseAccessService.CanAccessCaseAsync(caseId))
                return Results.NotFound();

            var caseEntity = await context.Cases.Include(@case => @case.Patient)
                .FirstOrDefaultAsync(@case => @case.Id == caseId);
            if (caseEntity?.Patient == null)
                return Results.NotFound();

            var patient = caseEntity.Patient;
            return Results.Json(new
            {
                addressLine = patient.AddressLine,
                city = patient.City,
                state = patient.State,
                postalCode = patient.PostalCode,
                country = "Australia",
                latitude = patient.Latitude,
                longitude = patient.Longitude
            });
        }).RequireAuthorization("Permission.Patient.View");

        app.MapGet("/api/users/search", (string? term, UserManager<ApplicationUser> userManager) =>
        {
            if (string.IsNullOrWhiteSpace(term) || term.Trim().Length < 2)
                return Results.Json(Array.Empty<object>());

            var normalizedTerm = term.Trim().ToUpperInvariant();
            var users = userManager.Users
                .Where(user => (user.UserName != null && user.UserName.ToUpper().Contains(normalizedTerm)) ||
                               (user.Email != null && user.Email.ToUpper().Contains(normalizedTerm)))
                .OrderBy(user => user.UserName)
                .Take(20)
                .Select(user => new
                {
                    id = user.Id,
                    text = user.UserName ?? user.Email ?? "Unknown user",
                    displayName = user.UserName ?? user.Email ?? "Unknown user"
                })
                .ToList();

            return Results.Json(users);
        }).RequireAuthorization();

        app.MapGet("/api/diseases/{id:guid}/exposure-requirements", async (Guid id, IExposureRequirementService service, ApplicationDbContext context) =>
        {
            // The globally filtered query prevents restricted disease configuration
            // leaking through an otherwise routine case-entry lookup.
            if (!await context.Diseases.AsNoTracking().AnyAsync(disease => disease.Id == id))
                return Results.NotFound();

            var disease = await service.GetRequirementsForDiseaseAsync(id);
            var shouldPrompt = await service.ShouldPromptForExposureAsync(id);
            return Results.Json(new
            {
                shouldPrompt,
                mode = disease?.ExposureTrackingMode.ToString(),
                guidanceText = disease?.ExposureGuidanceText,
                isRequired = disease?.ExposureTrackingMode is ExposureTrackingMode.LocalSpecificRegion or ExposureTrackingMode.OverseasAcquired,
                defaultToResidential = disease?.DefaultToResidentialAddress ?? false,
                requireCoordinates = disease?.RequireGeographicCoordinates ?? false,
                allowDomestic = disease?.AllowDomesticAcquisition ?? true
            });
        }).RequireAuthorization("Permission.Case.Create");

        return app;
    }

    private static int TryReadLimit(string? value, int defaultValue)
        => !string.IsNullOrWhiteSpace(value) && int.TryParse(value, out var parsed) ? parsed : defaultValue;
}
