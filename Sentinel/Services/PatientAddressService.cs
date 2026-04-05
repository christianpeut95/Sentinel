using Microsoft.EntityFrameworkCore;
using Sentinel.Data;
using Sentinel.Models;
using Sentinel.Models.Lookups;

namespace Sentinel.Services
{
    public class PatientAddressService : IPatientAddressService
    {
        private readonly ApplicationDbContext _context;
        private readonly IGeocodingService _geocodingService;
        private readonly ILogger<PatientAddressService> _logger;

        public PatientAddressService(
            ApplicationDbContext context,
            IGeocodingService geocodingService,
            ILogger<PatientAddressService> logger)
        {
            _context = context;
            _geocodingService = geocodingService;
            _logger = logger;
        }

        public async Task<PatientAddressUpdateResult> ProcessAddressChangeAsync(
            Patient patient,
            string? oldAddressLine,
            string? oldCity,
            int? oldStateId,
            string? oldPostalCode,
            string? currentUserId)
        {
            var result = new PatientAddressUpdateResult { Success = true };

            // Get old state code for logging
            string? oldStateCode = null;
            if (oldStateId.HasValue)
            {
                var oldState = await _context.States.FindAsync(oldStateId.Value);
                oldStateCode = oldState?.Code;
            }

            _logger.LogInformation(
                "ProcessAddressChangeAsync for Patient {PatientId}: Old={Old}, New={New}",
                patient.Id,
                FormatAddress(oldAddressLine, oldCity, oldStateCode, oldPostalCode),
                FormatAddress(patient.AddressLine, patient.City, patient.State?.Code, patient.PostalCode));

            // Check if address actually changed
            if (!HasAddressChanged(patient, oldAddressLine, oldCity, oldStateId, oldPostalCode))
            {
                _logger.LogInformation("No address change detected for Patient {PatientId}", patient.Id);
                result.AddressChanged = false;
                return result;
            }

            result.AddressChanged = true;

            // Geocode new address (server-side)
            var fullAddress = FormatAddress(patient.AddressLine, patient.City, patient.State?.Code, patient.PostalCode);
            if (!string.IsNullOrWhiteSpace(fullAddress))
            {
                try
                {
                    var (latitude, longitude) = await _geocodingService.GeocodeAsync(fullAddress);
                    if (latitude.HasValue && longitude.HasValue)
                    {
                        patient.Latitude = latitude;
                        patient.Longitude = longitude;
                        result.GeocodingSucceeded = true;
                        result.NewLatitude = latitude;
                        result.NewLongitude = longitude;
                        _logger.LogInformation(
                            "Geocoded Patient {PatientId} address: {Lat}, {Lon}",
                            patient.Id, latitude, longitude);
                    }
                    else
                    {
                        _logger.LogWarning("Geocoding returned null for Patient {PatientId} address: {Address}",
                            patient.Id, fullAddress);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Geocoding failed for Patient {PatientId} address: {Address}",
                        patient.Id, fullAddress);
                    result.Errors.Add($"Geocoding failed: {ex.Message}");
                }
            }

            // NOTE: Jurisdiction updates are handled by the background task in Edit.cshtml.cs
            // to avoid blocking the user during save (can take 1-2 minutes for point-in-polygon checks)

            // Find related cases
            var cases = await _context.Cases
                .Include(c => c.Disease)
                .Include(c => c.Jurisdiction1)
                .Include(c => c.Jurisdiction2)
                .Include(c => c.Jurisdiction3)
                .Include(c => c.Jurisdiction4)
                .Include(c => c.Jurisdiction5)
                .Where(c => c.PatientId == patient.Id && !c.IsDeleted)
                .ToListAsync();

            _logger.LogInformation("Found {Count} cases for Patient {PatientId}", cases.Count, patient.Id);

            // Store current (old) jurisdiction IDs from patient
            var oldPatientJurisdictions = new int?[] {
                patient.Jurisdiction1Id,
                patient.Jurisdiction2Id,
                patient.Jurisdiction3Id,
                patient.Jurisdiction4Id,
                patient.Jurisdiction5Id
            };

            foreach (var caseEntity in cases)
            {
                var disease = caseEntity.Disease;
                if (disease == null)
                {
                    _logger.LogWarning("Case {CaseId} has no disease - skipping", caseEntity.Id);
                    continue;
                }

                // Get effective address settings (with inheritance resolution)
                var settings = await GetEffectiveAddressSettingsAsync(disease.Id);

                // Check if disease is configured for address sync
                if (!settings.SyncWithPatientAddressUpdates)
                {
                    _logger.LogDebug(
                        "Disease {DiseaseName} (Case {CaseId}) has SyncWithPatientAddressUpdates=false - skipping",
                        disease.Name, caseEntity.Id);
                    continue;
                }

                // Check if case is within time window
                if (!await IsCaseWithinReviewWindowAsync(caseEntity, settings))
                {
                    _logger.LogDebug("Case {CaseId} is outside review time window - skipping",
                        caseEntity.Id);
                    continue;
                }

                // Check for manual override
                if (caseEntity.CaseAddressManualOverride)
                {
                    _logger.LogDebug("Case {CaseId} has manual address override - skipping auto-update",
                        caseEntity.Id);
                    
                    result.CasesRequiringReview.Add(new CaseAddressReviewItem
                    {
                        CaseId = caseEntity.Id,
                        CaseFriendlyId = caseEntity.FriendlyId,
                        DiseaseName = disease.Name,
                        DateOfOnset = caseEntity.DateOfOnset,
                        HasJurisdictionCrossing = false,
                        ManualOverrideExists = true,
                        OldAddress = FormatAddress(
                            caseEntity.CaseAddressLine,
                            caseEntity.CaseCity,
                            caseEntity.CaseState?.Code,
                            caseEntity.CasePostalCode),
                        NewAddress = fullAddress
                    });
                    continue;
                }

                // Get case's current jurisdiction IDs
                var caseJurisdictions = new int?[] {
                    caseEntity.Jurisdiction1Id,
                    caseEntity.Jurisdiction2Id,
                    caseEntity.Jurisdiction3Id,
                    caseEntity.Jurisdiction4Id,
                    caseEntity.Jurisdiction5Id
                };

                // Check for jurisdiction crossing
                var hasJurisdictionCrossing = settings.CheckJurisdictionCrossing &&
                    HasJurisdictionCrossing(
                        caseJurisdictions,
                        new int?[] {
                            patient.Jurisdiction1Id,
                            patient.Jurisdiction2Id,
                            patient.Jurisdiction3Id,
                            patient.Jurisdiction4Id,
                            patient.Jurisdiction5Id
                        },
                        settings.JurisdictionFieldsToCheck);

                if (hasJurisdictionCrossing)
                {
                    _logger.LogWarning(
                        "Jurisdiction crossing detected for Case {CaseId} - creating review queue entry",
                        caseEntity.Id);

                    // Create review queue entry for jurisdiction crossing
                    await CreateJurisdictionCrossingReviewAsync(
                        caseEntity,
                        patient,
                        oldAddressLine,
                        oldCity,
                        oldStateId,
                        oldPostalCode,
                        currentUserId);

                    result.CasesRequiringReview.Add(new CaseAddressReviewItem
                    {
                        CaseId = caseEntity.Id,
                        CaseFriendlyId = caseEntity.FriendlyId,
                        DiseaseName = disease.Name,
                        DateOfOnset = caseEntity.DateOfOnset,
                        HasJurisdictionCrossing = true,
                        ManualOverrideExists = false,
                        OldAddress = FormatAddress(
                            caseEntity.CaseAddressLine,
                            caseEntity.CaseCity,
                            caseEntity.CaseState?.Code,
                            caseEntity.CasePostalCode),
                        NewAddress = fullAddress,
                        OldJurisdiction = GetJurisdictionDisplay(caseJurisdictions, disease.JurisdictionFieldsToCheck),
                        NewJurisdiction = GetJurisdictionDisplay(new int?[] {
                            patient.Jurisdiction1Id,
                            patient.Jurisdiction2Id,
                            patient.Jurisdiction3Id,
                            patient.Jurisdiction4Id,
                            patient.Jurisdiction5Id
                        }, disease.JurisdictionFieldsToCheck)
                    });
                }
                else
                {
                    // No jurisdiction crossing - add to user prompt list (if user has permission)
                    result.CasesRequiringReview.Add(new CaseAddressReviewItem
                    {
                        CaseId = caseEntity.Id,
                        CaseFriendlyId = caseEntity.FriendlyId,
                        DiseaseName = disease.Name,
                        DateOfOnset = caseEntity.DateOfOnset,
                        HasJurisdictionCrossing = false,
                        ManualOverrideExists = false,
                        OldAddress = FormatAddress(
                            caseEntity.CaseAddressLine,
                            caseEntity.CaseCity,
                            caseEntity.CaseState?.Code,
                            caseEntity.CasePostalCode),
                        NewAddress = fullAddress
                    });
                }
            }

            return result;
        }

        public async Task CopyAddressToCaseAsync(Guid caseId, bool manualOverride = false)
        {
            var caseEntity = await _context.Cases
                .Include(c => c.Patient)
                .FirstOrDefaultAsync(c => c.Id == caseId);

            if (caseEntity?.Patient == null)
            {
                _logger.LogWarning("Case {CaseId} or patient not found", caseId);
                return;
            }

            _logger.LogInformation(
                "Copying address from Patient {PatientId} to Case {CaseId} (manualOverride={Override})",
                caseEntity.PatientId, caseId, manualOverride);

            caseEntity.CaseAddressLine = caseEntity.Patient.AddressLine;
            caseEntity.CaseCity = caseEntity.Patient.City;
            caseEntity.CaseStateId = caseEntity.Patient.StateId;
            caseEntity.CasePostalCode = caseEntity.Patient.PostalCode;
            caseEntity.CaseLatitude = caseEntity.Patient.Latitude;
            caseEntity.CaseLongitude = caseEntity.Patient.Longitude;
            caseEntity.CaseAddressCapturedAt = DateTime.UtcNow;
            caseEntity.CaseAddressManualOverride = manualOverride;

            // Copy jurisdiction IDs
            caseEntity.Jurisdiction1Id = caseEntity.Patient.Jurisdiction1Id;
            caseEntity.Jurisdiction2Id = caseEntity.Patient.Jurisdiction2Id;
            caseEntity.Jurisdiction3Id = caseEntity.Patient.Jurisdiction3Id;
            caseEntity.Jurisdiction4Id = caseEntity.Patient.Jurisdiction4Id;
            caseEntity.Jurisdiction5Id = caseEntity.Patient.Jurisdiction5Id;

            await _context.SaveChangesAsync();

            _logger.LogInformation("Address copied to Case {CaseId}", caseId);
        }

        public async Task ApplyAddressToCasesAsync(Guid patientId, List<Guid> caseIds, string? currentUserId)
        {
            var patient = await _context.Patients.FindAsync(patientId);
            if (patient == null)
            {
                _logger.LogWarning("Patient {PatientId} not found", patientId);
                return;
            }

            var cases = await _context.Cases
                .Where(c => caseIds.Contains(c.Id) && c.PatientId == patientId)
                .ToListAsync();

            _logger.LogInformation(
                "Applying Patient {PatientId} address to {Count} cases",
                patientId, cases.Count);

            foreach (var caseEntity in cases)
            {
                caseEntity.CaseAddressLine = patient.AddressLine;
                caseEntity.CaseCity = patient.City;
                caseEntity.CaseStateId = patient.StateId;
                caseEntity.CasePostalCode = patient.PostalCode;
                caseEntity.CaseLatitude = patient.Latitude;
                caseEntity.CaseLongitude = patient.Longitude;
                caseEntity.CaseAddressCapturedAt = DateTime.UtcNow;
                caseEntity.CaseAddressManualOverride = false; // User confirmed, not a manual override

                // Copy jurisdiction IDs
                caseEntity.Jurisdiction1Id = patient.Jurisdiction1Id;
                caseEntity.Jurisdiction2Id = patient.Jurisdiction2Id;
                caseEntity.Jurisdiction3Id = patient.Jurisdiction3Id;
                caseEntity.Jurisdiction4Id = patient.Jurisdiction4Id;
                caseEntity.Jurisdiction5Id = patient.Jurisdiction5Id;
            }

            await _context.SaveChangesAsync();

            _logger.LogInformation("Address applied to {Count} cases", cases.Count);
        }

        private async Task<bool> IsCaseWithinReviewWindowAsync(Case caseEntity, DiseaseAddressSettings settings)
        {
            var disease = caseEntity.Disease ?? await _context.Diseases.FindAsync(caseEntity.DiseaseId);
            if (disease == null)
            {
                _logger.LogWarning("Disease not found for Case {CaseId}", caseEntity.Id);
                return false;
            }

            // If no time windows configured, include all cases
            if (!settings.AddressReviewWindowBeforeDays.HasValue &&
                !settings.AddressReviewWindowAfterDays.HasValue)
            {
                _logger.LogDebug("No time window configured for Disease {DiseaseName} - including Case {CaseId}",
                    disease.Name, caseEntity.Id);
                return true;
            }

            // If no onset date, cannot determine time window
            if (!caseEntity.DateOfOnset.HasValue)
            {
                _logger.LogDebug("Case {CaseId} has no DateOfOnset - excluding from review", caseEntity.Id);
                return false;
            }

            var now = DateTime.UtcNow.Date;
            var onset = caseEntity.DateOfOnset.Value.Date;

            var beforeDays = settings.AddressReviewWindowBeforeDays ?? 0;
            var afterDays = settings.AddressReviewWindowAfterDays ?? 0;

            var windowStart = onset.AddDays(-beforeDays);
            var windowEnd = onset.AddDays(afterDays);

            var isWithinWindow = now >= windowStart && now <= windowEnd;

            _logger.LogDebug(
                "Case {CaseId} onset={Onset}, window={Start} to {End}, now={Now}, withinWindow={Within}",
                caseEntity.Id, onset, windowStart, windowEnd, now, isWithinWindow);

            return isWithinWindow;
        }

        public bool HasJurisdictionCrossing(
            int?[] oldJurisdictions,
            int?[] newJurisdictions,
            string? fieldsToCheck)
        {
            if (string.IsNullOrWhiteSpace(fieldsToCheck))
            {
                // Default to checking all fields
                fieldsToCheck = "1,2,3,4,5";
            }

            var fieldNumbers = fieldsToCheck
                .Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(f => int.TryParse(f.Trim(), out var num) ? num : (int?)null)
                .Where(n => n.HasValue && n.Value >= 1 && n.Value <= 5)
                .Select(n => n!.Value - 1) // Convert to 0-based index
                .ToList();

            foreach (var index in fieldNumbers)
            {
                var oldValue = oldJurisdictions.ElementAtOrDefault(index);
                var newValue = newJurisdictions.ElementAtOrDefault(index);

                if (oldValue != newValue)
                {
                    _logger.LogDebug(
                        "Jurisdiction crossing detected at field {Field}: {Old} -> {New}",
                        index + 1, oldValue, newValue);
                    return true;
                }
            }

            return false;
        }

        public async Task<DiseaseAddressSettings> GetEffectiveAddressSettingsAsync(Guid diseaseId)
        {
            var disease = await _context.Diseases
                .AsNoTracking()
                .FirstOrDefaultAsync(d => d.Id == diseaseId);

            if (disease == null)
            {
                throw new ArgumentException($"Disease {diseaseId} not found", nameof(diseaseId));
            }

            // If not inheriting or no parent, use current disease settings
            if (!disease.InheritAddressSettingsFromParent || disease.ParentDiseaseId == null)
            {
                return new DiseaseAddressSettings
                {
                    SyncWithPatientAddressUpdates = disease.SyncWithPatientAddressUpdates,
                    AddressReviewWindowBeforeDays = disease.AddressReviewWindowBeforeDays,
                    AddressReviewWindowAfterDays = disease.AddressReviewWindowAfterDays,
                    CheckJurisdictionCrossing = disease.CheckJurisdictionCrossing,
                    JurisdictionFieldsToCheck = disease.JurisdictionFieldsToCheck,
                    DefaultToResidentialAddress = disease.DefaultToResidentialAddress,
                    SourceDiseaseId = disease.Id,
                    IsInherited = false
                };
            }

            // Walk up PathIds to find first ancestor with inheritance disabled
            if (!string.IsNullOrWhiteSpace(disease.PathIds))
            {
                var ancestorIds = disease.PathIds
                    .Split('/', StringSplitOptions.RemoveEmptyEntries)
                    .Select(id => Guid.TryParse(id, out var guid) ? guid : (Guid?)null)
                    .Where(id => id.HasValue)
                    .Select(id => id!.Value)
                    .Reverse() // Start from immediate parent
                    .ToList();

                foreach (var ancestorId in ancestorIds)
                {
                    var ancestor = await _context.Diseases
                        .AsNoTracking()
                        .FirstOrDefaultAsync(d => d.Id == ancestorId);

                    if (ancestor != null && !ancestor.InheritAddressSettingsFromParent)
                    {
                        _logger.LogDebug(
                            "Disease {DiseaseId} ({DiseaseName}) inheriting address settings from {AncestorId} ({AncestorName})",
                            disease.Id, disease.Name, ancestor.Id, ancestor.Name);

                        return new DiseaseAddressSettings
                        {
                            SyncWithPatientAddressUpdates = ancestor.SyncWithPatientAddressUpdates,
                            AddressReviewWindowBeforeDays = ancestor.AddressReviewWindowBeforeDays,
                            AddressReviewWindowAfterDays = ancestor.AddressReviewWindowAfterDays,
                            CheckJurisdictionCrossing = ancestor.CheckJurisdictionCrossing,
                            JurisdictionFieldsToCheck = ancestor.JurisdictionFieldsToCheck,
                            DefaultToResidentialAddress = ancestor.DefaultToResidentialAddress,
                            SourceDiseaseId = ancestor.Id,
                            IsInherited = true
                        };
                    }
                }
            }

            // Fallback to current disease if no suitable ancestor found
            _logger.LogWarning(
                "Disease {DiseaseId} ({DiseaseName}) configured to inherit but no suitable parent found - using own settings",
                disease.Id, disease.Name);

            return new DiseaseAddressSettings
            {
                SyncWithPatientAddressUpdates = disease.SyncWithPatientAddressUpdates,
                AddressReviewWindowBeforeDays = disease.AddressReviewWindowBeforeDays,
                AddressReviewWindowAfterDays = disease.AddressReviewWindowAfterDays,
                CheckJurisdictionCrossing = disease.CheckJurisdictionCrossing,
                JurisdictionFieldsToCheck = disease.JurisdictionFieldsToCheck,
                DefaultToResidentialAddress = disease.DefaultToResidentialAddress,
                SourceDiseaseId = disease.Id,
                IsInherited = false
            };
        }

        // Private helper methods

        private async Task UpdatePatientJurisdictionsAsync(Patient patient)
        {
            if (!patient.Latitude.HasValue || !patient.Longitude.HasValue)
            {
                _logger.LogDebug("Patient {PatientId} has no coordinates - skipping jurisdiction update", patient.Id);
                return;
            }

            _logger.LogInformation("Updating jurisdictions for Patient {PatientId} at ({Lat}, {Lon})",
                patient.Id, patient.Latitude, patient.Longitude);

            // Get all active jurisdiction types
            var jurisdictionTypes = await _context.JurisdictionTypes
                .Where(jt => jt.IsActive)
                .OrderBy(jt => jt.DisplayOrder)
                .ToListAsync();

            foreach (var jurisdictionType in jurisdictionTypes)
            {
                // Find jurisdictions that contain this point
                var matchingJurisdictions = await _context.Jurisdictions
                    .Where(j => j.JurisdictionTypeId == jurisdictionType.Id && j.IsActive)
                    .ToListAsync();

                Jurisdiction? matchedJurisdiction = null;

                foreach (var jurisdiction in matchingJurisdictions)
                {
                    if (!string.IsNullOrEmpty(jurisdiction.BoundaryData))
                    {
                        if (IsPointInJurisdiction(patient.Latitude.Value, patient.Longitude.Value, jurisdiction.BoundaryData))
                        {
                            matchedJurisdiction = jurisdiction;
                            break;
                        }
                    }
                }

                // Update the appropriate jurisdiction field
                if (matchedJurisdiction != null)
                {
                    switch (jurisdictionType.FieldNumber)
                    {
                        case 1:
                            patient.Jurisdiction1Id = matchedJurisdiction.Id;
                            _logger.LogInformation("Set Jurisdiction1 = {Name} for Patient {PatientId}",
                                matchedJurisdiction.Name, patient.Id);
                            break;
                        case 2:
                            patient.Jurisdiction2Id = matchedJurisdiction.Id;
                            _logger.LogInformation("Set Jurisdiction2 = {Name} for Patient {PatientId}",
                                matchedJurisdiction.Name, patient.Id);
                            break;
                        case 3:
                            patient.Jurisdiction3Id = matchedJurisdiction.Id;
                            _logger.LogInformation("Set Jurisdiction3 = {Name} for Patient {PatientId}",
                                matchedJurisdiction.Name, patient.Id);
                            break;
                        case 4:
                            patient.Jurisdiction4Id = matchedJurisdiction.Id;
                            _logger.LogInformation("Set Jurisdiction4 = {Name} for Patient {PatientId}",
                                matchedJurisdiction.Name, patient.Id);
                            break;
                        case 5:
                            patient.Jurisdiction5Id = matchedJurisdiction.Id;
                            _logger.LogInformation("Set Jurisdiction5 = {Name} for Patient {PatientId}",
                                matchedJurisdiction.Name, patient.Id);
                            break;
                    }
                }
            }
        }

        private bool IsPointInJurisdiction(double latitude, double longitude, string geometryJson)
        {
            try
            {
                using var jsonDoc = System.Text.Json.JsonDocument.Parse(geometryJson);
                var root = jsonDoc.RootElement;

                if (!root.TryGetProperty("type", out var typeElement) || typeElement.GetString() != "Polygon")
                    return false;

                if (!root.TryGetProperty("coordinates", out var coordsElement))
                    return false;

                // Get the outer ring (first array in coordinates)
                var outerRing = coordsElement[0];
                var polygon = new List<(double lon, double lat)>();

                foreach (var coord in outerRing.EnumerateArray())
                {
                    if (coord.GetArrayLength() >= 2)
                    {
                        var lon = coord[0].GetDouble();
                        var lat = coord[1].GetDouble();
                        polygon.Add((lon, lat));
                    }
                }

                return IsPointInPolygon(latitude, longitude, polygon);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to parse geometry JSON");
                return false;
            }
        }

        private bool IsPointInPolygon(double latitude, double longitude, List<(double lon, double lat)> polygon)
        {
            // Ray casting algorithm for point-in-polygon test
            bool inside = false;

            for (int i = 0, j = polygon.Count - 1; i < polygon.Count; j = i++)
            {
                var (xi, yi) = polygon[i];
                var (xj, yj) = polygon[j];

                bool intersect = ((yi > latitude) != (yj > latitude))
                    && (longitude < (xj - xi) * (latitude - yi) / (yj - yi) + xi);

                if (intersect) inside = !inside;
            }

            return inside;
        }

        private bool HasAddressChanged(
            Patient patient,
            string? oldAddressLine,
            string? oldCity,
            int? oldStateId,
            string? oldPostalCode)
        {
            return patient.AddressLine != oldAddressLine ||
                   patient.City != oldCity ||
                   patient.StateId != oldStateId ||
                   patient.PostalCode != oldPostalCode;
        }

        private string FormatAddress(string? line, string? city, string? state, string? postcode)
        {
            var parts = new[] { line, city, state, postcode }
                .Where(p => !string.IsNullOrWhiteSpace(p));
            return string.Join(", ", parts);
        }

        private async Task CreateJurisdictionCrossingReviewAsync(
            Case caseEntity,
            Patient patient,
            string? oldAddressLine,
            string? oldCity,
            int? oldStateId,
            string? oldPostalCode,
            string? currentUserId)
        {
            // Get old state code for JSON snapshot
            string? oldStateCode = null;
            if (oldStateId.HasValue)
            {
                var oldState = await _context.States.FindAsync(oldStateId.Value);
                oldStateCode = oldState?.Code;
            }

            var changeSnapshot = System.Text.Json.JsonSerializer.Serialize(new
            {
                patientId = patient.Id,
                caseId = caseEntity.Id,
                oldAddress = new
                {
                    line = oldAddressLine,
                    city = oldCity,
                    state = oldStateCode,
                    postcode = oldPostalCode,
                    jurisdiction1 = caseEntity.Jurisdiction1Id,
                    jurisdiction2 = caseEntity.Jurisdiction2Id,
                    jurisdiction3 = caseEntity.Jurisdiction3Id,
                    jurisdiction4 = caseEntity.Jurisdiction4Id,
                    jurisdiction5 = caseEntity.Jurisdiction5Id
                },
                newAddress = new
                {
                    line = patient.AddressLine,
                    city = patient.City,
                    state = patient.State?.Code,
                    postcode = patient.PostalCode,
                    latitude = patient.Latitude,
                    longitude = patient.Longitude,
                    jurisdiction1 = patient.Jurisdiction1Id,
                    jurisdiction2 = patient.Jurisdiction2Id,
                    jurisdiction3 = patient.Jurisdiction3Id,
                    jurisdiction4 = patient.Jurisdiction4Id,
                    jurisdiction5 = patient.Jurisdiction5Id
                },
                changedAt = DateTime.UtcNow,
                changedBy = currentUserId
            });

            var reviewEntry = new ReviewQueue
            {
                EntityType = "JurisdictionCrossing",
                EntityId = caseEntity.Id.GetHashCode(),
                CaseId = caseEntity.Id,
                PatientId = patient.Id,
                DiseaseId = caseEntity.DiseaseId,
                ChangeType = "AddressChanged",
                TriggerField = "PatientAddress",
                ChangeSnapshot = changeSnapshot,
                Priority = 1, // High priority for jurisdiction crossings
                ReviewStatus = "Pending",
                GroupKey = $"JurisdictionCrossing|{caseEntity.Id}|{DateTime.UtcNow:yyyyMMdd}",
                GroupCount = 1,
                CreatedDate = DateTime.UtcNow
            };

            _context.ReviewQueue.Add(reviewEntry);

            _logger.LogInformation(
                "Created review queue entry for jurisdiction crossing: Case {CaseId}",
                caseEntity.Id);
        }

        private string? GetJurisdictionDisplay(int?[] jurisdictionIds, string? fieldsToCheck)
        {
            if (string.IsNullOrWhiteSpace(fieldsToCheck))
                return null;

            var fieldNumbers = fieldsToCheck
                .Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(f => int.TryParse(f.Trim(), out var num) ? num : (int?)null)
                .Where(n => n.HasValue && n.Value >= 1 && n.Value <= 5)
                .Select(n => n!.Value - 1) // Convert to 0-based index
                .ToList();

            var values = fieldNumbers
                .Select(idx => jurisdictionIds.ElementAtOrDefault(idx))
                .Where(id => id.HasValue)
                .Select(id => id!.Value.ToString())
                .ToList();

            return values.Any() ? string.Join(", ", values) : null;
        }
    }
}
