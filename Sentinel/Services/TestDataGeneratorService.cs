using Microsoft.EntityFrameworkCore;
using Sentinel.Data;
using Sentinel.Models;
using Sentinel.Models.Lookups;
using Microsoft.Extensions.Configuration;

namespace Sentinel.Services
{
    public class TestDataGeneratorService
    {
        private readonly ApplicationDbContext _context;
        private readonly IGeocodingService _geocodingService;
        private readonly IJurisdictionService _jurisdictionService;
        private readonly IConfiguration _configuration;
        private readonly IPatientIdGeneratorService _patientIdGenerator;
        private readonly Random _random = new Random();

        // 70 Australian first names
        private static readonly string[] FirstNames = 
        {
            "Oliver", "Charlotte", "Noah", "Amelia", "Jack", "Isla", "William", "Olivia",
            "Thomas", "Mia", "James", "Ava", "Lucas", "Grace", "Henry", "Chloe",
            "Ethan", "Sophie", "Mason", "Zoe", "Alexander", "Emily", "Liam", "Ella",
            "Benjamin", "Ruby", "Samuel", "Matilda", "Joshua", "Harper", "Daniel", "Lily",
            "Charlie", "Evie", "Matthew", "Lucy", "Max", "Sophia", "Ryan", "Alice",
            "Leo", "Isabella", "Harrison", "Eva", "Cooper", "Hannah", "Archie", "Scarlett",
            "Hunter", "Aria", "Isaac", "Willow", "Oscar", "Poppy", "Levi", "Layla",
            "Xavier", "Violet", "Elijah", "Sienna", "Hudson", "Madison", "Tyler", "Audrey",
            "Sebastian", "Maya", "Ashton", "Bella", "Jayden", "Jasmine", "Blake", "Piper"
        };

        // 70 Australian last names
        private static readonly string[] LastNames = 
        {
            "Smith", "Jones", "Williams", "Brown", "Wilson", "Taylor", "Johnson", "White",
            "Martin", "Anderson", "Thompson", "Nguyen", "Thomas", "Walker", "Harris", "Lee",
            "Ryan", "Robinson", "Kelly", "King", "Davis", "Wright", "Evans", "Roberts",
            "Green", "Hall", "Wood", "Jackson", "Clarke", "Patel", "Nguyen", "Mitchell",
            "Baker", "Adams", "Hill", "Singh", "Campbell", "Allen", "Turner", "Parker",
            "Scott", "Murray", "Young", "Moore", "Clark", "Cooper", "Morris", "Watson",
            "Rogers", "Reed", "Bailey", "Morgan", "Bell", "Murphy", "Collins", "Cook",
            "Richardson", "Edwards", "Stewart", "Phillips", "Long", "Hughes", "Price", "Russell",
            "Bennett", "Perry", "Powell", "Patterson", "Barnes", "Griffin", "Butler", "Henderson"
        };

        // Adelaide suburbs for realistic addresses
        private static readonly string[] AdelaideSuburbs = 
        {
            "Adelaide", "North Adelaide", "Unley", "Norwood", "Prospect", "Glenelg",
            "Brighton", "Henley Beach", "Port Adelaide", "Woodville", "Salisbury", "Elizabeth",
            "Modbury", "Tea Tree Gully", "Campbelltown", "Paradise", "Athelstone", "Newton",
            "Payneham", "Kensington", "Burnside", "Magill", "Mitcham", "Marion", "Morphett Vale",
            "Noarlunga", "Aberfoyle Park", "Blackwood", "Belair", "Stirling", "Mount Barker",
            "Gawler", "Angle Vale", "Virginia", "Munno Para", "Seaford", "Christies Beach",
            "Hackham", "Reynella", "Hallett Cove", "Sheidow Park", "Flagstaff Hill", "Coromandel Valley"
        };

        private static readonly string[] StreetNames = 
        {
            "King William", "North Terrace", "Rundle", "Hindley", "Grote", "Waymouth",
            "Currie", "Flinders", "Franklin", "Angas", "Pirie", "Gouger", "Halifax",
            "Morphett", "Grenfell", "Gawler", "Whitmore", "Hutt", "Sturt", "Henley Beach",
            "Port", "Churchill", "Grand Junction", "South", "West", "East", "Main",
            "Station", "Commercial", "Bridge", "Park", "Beach", "Ocean", "River",
            "Hill", "Valley", "Forest", "Garden", "Lake", "Creek", "Ridge"
        };

        private static readonly string[] StreetTypes = 
        {
            "Street", "Road", "Avenue", "Drive", "Terrace", "Place", "Court", "Crescent",
            "Lane", "Way", "Boulevard", "Parade", "Highway", "Grove"
        };

        public TestDataGeneratorService(
            ApplicationDbContext context,
            IGeocodingService geocodingService,
            IJurisdictionService jurisdictionService,
            IConfiguration configuration,
            IPatientIdGeneratorService patientIdGenerator)
        {
            _context = context;
            _geocodingService = geocodingService;
            _jurisdictionService = jurisdictionService;
            _configuration = configuration;
            _patientIdGenerator = patientIdGenerator;
        }

        public async Task<TestDataGenerationResult> GeneratePatientsAsync(
            int count,
            bool useGeocoding = true,
            Action<string>? progressCallback = null)
        {
            var result = new TestDataGenerationResult();
            var orgConfig = _configuration.GetSection("Organization");
            var state = orgConfig["State"] ?? "South Australia";
            var country = orgConfig["Country"] ?? "Australia";

            progressCallback?.Invoke($"Loading demographic lookup data...");

            // Load all demographic lookup data once at the start
            var sexAtBirths = await _context.SexAtBirths.Where(s => s.IsActive).ToListAsync();
            var genders = await _context.Genders.Where(g => g.IsActive).ToListAsync();
            var atsiStatuses = await _context.AtsiStatuses.Where(a => a.IsActive).ToListAsync();
            var occupations = await _context.Occupations.Where(o => o.IsActive).Take(100).ToListAsync();
            var countries = await _context.Countries.Take(50).ToListAsync(); // Top 50 countries
            var languages = await _context.Languages.Take(30).ToListAsync(); // Top 30 languages
            var ancestries = await _context.Ancestries.Take(20).ToListAsync(); // Top 20 ancestries

            progressCallback?.Invoke($"Starting generation of {count} patients...");

            for (int i = 0; i < count; i++)
            {
                try
                {
                    // Generate unique ID BEFORE creating patient to avoid duplicates
                    var friendlyId = await _patientIdGenerator.GenerateNextPatientIdAsync();
                    
                    var patient = await GenerateSinglePatientAsync(
                        state, 
                        country, 
                        useGeocoding,
                        sexAtBirths,
                        genders,
                        atsiStatuses,
                        occupations,
                        countries,
                        languages,
                        ancestries);
                    
                    patient.FriendlyId = friendlyId; // Use the pre-generated ID
                    
                    _context.Patients.Add(patient);
                    result.PatientsCreated++;

                    if ((i + 1) % 10 == 0)
                    {
                        await _context.SaveChangesAsync();
                        progressCallback?.Invoke($"Created {i + 1}/{count} patients...");
                    }
                }
                catch (Exception ex)
                {
                    result.Errors.Add($"Patient {i + 1}: {ex.Message}");
                }
            }

            // Save any remaining patients
            await _context.SaveChangesAsync();
            
            progressCallback?.Invoke($"? Successfully created {result.PatientsCreated} patients");
            return result;
        }

        private async Task<Patient> GenerateSinglePatientAsync(
            string state, 
            string country, 
            bool useGeocoding,
            List<Models.Lookups.SexAtBirth> sexAtBirths,
            List<Models.Lookups.Gender> genders,
            List<Models.Lookups.AboriginalTorresStraitIslanderStatus> atsiStatuses,
            List<Models.Lookups.Occupation> occupations,
            List<Models.Lookups.Country> countries,
            List<Models.Lookups.Language> languages,
            List<Models.Lookups.Ancestry> ancestries)
        {
            var firstName = GetRandom(FirstNames);
            var lastName = GetRandom(LastNames);
            var dateOfBirth = GenerateRandomDateOfBirth();
            var address = GenerateAustralianAddress();

            var patient = new Patient
            {
                GivenName = firstName,
                FamilyName = lastName,
                DateOfBirth = dateOfBirth,
                MobilePhone = GenerateAustralianPhone(),
                HomePhone = _random.Next(0, 100) < 70 ? GenerateAustralianPhone() : null, // 70% have home phone
                EmailAddress = GenerateEmail(firstName, lastName),
                AddressLine = address.Street,
                City = address.Suburb,
                State = state,
                PostalCode = address.Postcode,
                IsDeceased = false,
                
                // Assign demographic lookups (with some nulls for realism)
                SexAtBirthId = sexAtBirths.Any() && _random.Next(0, 100) < 95 ? GetRandomOrNull(sexAtBirths)?.Id : null, // 95% provide
                GenderId = genders.Any() && _random.Next(0, 100) < 90 ? GetRandomOrNull(genders)?.Id : null, // 90% provide
                AtsiStatusId = atsiStatuses.Any() && _random.Next(0, 100) < 85 ? GetRandomOrNull(atsiStatuses)?.Id : null, // 85% provide
                OccupationId = occupations.Any() && _random.Next(0, 100) < 70 ? GetRandomOrNull(occupations)?.Id : null, // 70% provide
                CountryOfBirthId = countries.Any() && _random.Next(0, 100) < 80 ? GetRandomOrNull(countries)?.Id : null, // 80% provide
                LanguageSpokenAtHomeId = languages.Any() && _random.Next(0, 100) < 75 ? GetRandomOrNull(languages)?.Id : null, // 75% provide
                AncestryId = ancestries.Any() && _random.Next(0, 100) < 70 ? GetRandomOrNull(ancestries)?.Id : null // 70% provide
            };

            // Geocode and assign jurisdiction if enabled
            if (useGeocoding)
            {
                try
                {
                    var fullAddress = $"{address.Street}, {address.Suburb}, {state}, {address.Postcode}, {country}";
                    var (lat, lon) = await _geocodingService.GeocodeAsync(fullAddress);

                    if (lat.HasValue && lon.HasValue)
                    {
                        patient.Latitude = lat.Value;
                        patient.Longitude = lon.Value;

                        // Try to assign jurisdiction based on coordinates
                        var jurisdictions = await _jurisdictionService.FindJurisdictionsContainingPointAsync(lat.Value, lon.Value);
                        if (jurisdictions.Any())
                        {
                            // Assign jurisdictions based on their field numbers
                            foreach (var jurisdiction in jurisdictions.OrderBy(j => j.JurisdictionType?.FieldNumber))
                            {
                                switch (jurisdiction.JurisdictionType?.FieldNumber)
                                {
                                    case 1:
                                        patient.Jurisdiction1Id = jurisdiction.Id;
                                        break;
                                    case 2:
                                        patient.Jurisdiction2Id = jurisdiction.Id;
                                        break;
                                    case 3:
                                        patient.Jurisdiction3Id = jurisdiction.Id;
                                        break;
                                    case 4:
                                        patient.Jurisdiction4Id = jurisdiction.Id;
                                        break;
                                    case 5:
                                        patient.Jurisdiction5Id = jurisdiction.Id;
                                        break;
                                }
                            }
                        }
                    }
                }
                catch (Exception)
                {
                    // Geocoding failed, continue without coordinates
                }
            }

            return patient;
        }

        private DateTime GenerateRandomDateOfBirth()
        {
            // Generate ages from 0 to 100 years old
            var maxDaysOld = 365 * 100;
            var minDaysOld = 0;
            var daysOld = _random.Next(minDaysOld, maxDaysOld);
            return DateTime.UtcNow.AddDays(-daysOld).Date;
        }

        private (string Street, string Suburb, string Postcode) GenerateAustralianAddress()
        {
            var streetNumber = _random.Next(1, 999);
            var streetName = GetRandom(StreetNames);
            var streetType = GetRandom(StreetTypes);
            var suburb = GetRandom(AdelaideSuburbs);
            
            // Generate realistic Adelaide postcodes (5000-5999)
            var postcode = _random.Next(5000, 5999).ToString();

            return (
                Street: $"{streetNumber} {streetName} {streetType}",
                Suburb: suburb,
                Postcode: postcode
            );
        }

        private string GenerateAustralianPhone()
        {
            // Australian mobile format: 04XX XXX XXX
            var formats = new[]
            {
                $"04{_random.Next(10, 99)} {_random.Next(100, 999)} {_random.Next(100, 999)}", // Mobile
                $"08 {_random.Next(1000, 9999)} {_random.Next(1000, 9999)}", // Adelaide landline
            };

            return GetRandom(formats);
        }

        private string GenerateEmail(string firstName, string lastName)
        {
            var domains = new[] { "gmail.com", "outlook.com", "yahoo.com", "icloud.com", "hotmail.com", "example.com" };
            var formats = new[]
            {
                $"{firstName.ToLower()}.{lastName.ToLower()}@{GetRandom(domains)}",
                $"{firstName.ToLower()}{lastName.ToLower()}@{GetRandom(domains)}",
                $"{firstName.ToLower()}{_random.Next(1, 999)}@{GetRandom(domains)}",
                $"{firstName[0].ToString().ToLower()}{lastName.ToLower()}@{GetRandom(domains)}"
            };

            return GetRandom(formats);
        }

        private T GetRandom<T>(T[] array)
        {
            return array[_random.Next(array.Length)];
        }

        private T? GetRandomOrNull<T>(List<T> list) where T : class
        {
            if (list == null || !list.Any())
                return null;
            
            return list[_random.Next(list.Count)];
        }

        public async Task<TestDataGenerationResult> GenerateCasesAsync(
            int startYear,
            int endYear,
            int casesPerYear,
            List<Guid>? diseaseIds = null,
            bool includeCustomFields = true,
            bool includeLabResults = false,
            bool useSeasonalPatterns = true,
            Action<string>? progressCallback = null)
        {
            var result = new TestDataGenerationResult();

            progressCallback?.Invoke("Loading diseases and lookup data...");

            var diseases = diseaseIds != null && diseaseIds.Any()
                ? await _context.Diseases.Where(d => diseaseIds.Contains(d.Id) && d.IsActive).ToListAsync()
                : await _context.Diseases.Where(d => d.IsActive).ToListAsync();

            if (!diseases.Any())
            {
                result.Errors.Add("No active diseases found");
                return result;
            }

            var caseStatuses = await _context.CaseStatuses.Where(cs => cs.IsActive).ToListAsync();
            var hospitals = await _context.Organizations.Where(o => o.IsActive).Take(10).ToListAsync();
            var patients = await _context.Patients.OrderBy(p => Guid.NewGuid()).Take(casesPerYear * (endYear - startYear + 1)).ToListAsync();
            var jurisdictions = await _context.Jurisdictions.Where(j => j.IsActive).ToListAsync();

            if (!patients.Any())
            {
                result.Errors.Add("No patients found. Please generate patients first.");
                return result;
            }

            progressCallback?.Invoke($"Generating cases from {startYear} to {endYear}...");

            int patientIndex = 0;
            int totalYears = endYear - startYear + 1;

            for (int year = startYear; year <= endYear; year++)
            {
                for (int i = 0; i < casesPerYear; i++)
                {
                    try
                    {
                        if (patientIndex >= patients.Count)
                        {
                            result.Errors.Add($"Not enough patients available. Generated {result.CasesCreated} cases.");
                            await _context.SaveChangesAsync();
                            return result;
                        }

                        var patient = patients[patientIndex++];
                        var disease = GetRandom(diseases.ToArray());
                        var dateOfOnset = GenerateRandomDateInYear(year, disease, useSeasonalPatterns);

                        var caseEntity = await GenerateSingleCaseAsync(
                            patient,
                            disease,
                            dateOfOnset,
                            caseStatuses,
                            hospitals,
                            jurisdictions);

                        _context.Cases.Add(caseEntity);
                        result.CasesCreated++;

                        if (includeLabResults && _random.Next(0, 100) < 60)
                        {
                            var labResult = await GenerateLabResultForCaseAsync(caseEntity, dateOfOnset);
                            _context.LabResults.Add(labResult);
                            result.LabResultsCreated++;
                        }

                        if ((result.CasesCreated) % 50 == 0)
                        {
                            await _context.SaveChangesAsync();
                            progressCallback?.Invoke($"Created {result.CasesCreated} cases ({year})...");
                        }
                    }
                    catch (Exception ex)
                    {
                        result.Errors.Add($"Year {year}, Case {i + 1}: {ex.Message}");
                    }
                }

                await _context.SaveChangesAsync();
                progressCallback?.Invoke($"? Completed {year}: {casesPerYear} cases");
            }

            if (includeCustomFields)
            {
                progressCallback?.Invoke("Generating custom field values...");
                var allCases = await _context.Cases
                    .Where(c => c.DateOfOnset.HasValue &&
                                c.DateOfOnset.Value.Year >= startYear &&
                                c.DateOfOnset.Value.Year <= endYear &&
                                c.DiseaseId.HasValue)
                    .ToListAsync();

                foreach (var caseEntity in allCases)
                {
                    try
                    {
                        await GenerateCustomFieldValuesForCaseAsync(caseEntity.Id, caseEntity.DiseaseId!.Value);
                        result.CustomFieldsCreated++;
                    }
                    catch (Exception ex)
                    {
                        result.Errors.Add($"Custom fields for {caseEntity.FriendlyId}: {ex.Message}");
                    }
                }

                await _context.SaveChangesAsync();
                progressCallback?.Invoke($"? Generated custom fields for {result.CustomFieldsCreated} cases");
            }

            progressCallback?.Invoke($"? Successfully created {result.CasesCreated} cases across {totalYears} years");
            return result;
        }

        private async Task<Case> GenerateSingleCaseAsync(
            Patient patient,
            Disease disease,
            DateTime dateOfOnset,
            List<CaseStatus> caseStatuses,
            List<Organization> hospitals,
            List<Jurisdiction> jurisdictions)
        {
            var friendlyId = await GenerateUniqueCaseIdAsync(dateOfOnset.Year);

            var dateOfNotification = dateOfOnset.AddDays(_random.Next(1, 8));
            var clinicalNotificationDate = _random.Next(0, 100) < 40
                ? dateOfOnset.AddDays(_random.Next(0, 4))
                : (DateTime?)null;

            var confirmationStatus = caseStatuses.Any() ? GetRandomOrNull(caseStatuses) : null;
            var hospitalized = (YesNoUnknown)_random.Next(0, 3);
            var hospital = hospitalized == YesNoUnknown.Yes && hospitals.Any() ? GetRandomOrNull(hospitals) : null;
            DateTime? admissionDate = null;
            DateTime? dischargeDate = null;

            if (hospitalized == YesNoUnknown.Yes && hospital != null)
            {
                admissionDate = dateOfOnset.AddDays(_random.Next(0, 5));
                dischargeDate = admissionDate.Value.AddDays(_random.Next(3, 21));
            }

            var died = hospitalized == YesNoUnknown.Yes && _random.Next(0, 100) < 5
                ? YesNoUnknown.Yes
                : YesNoUnknown.No;

            var caseEntity = new Case
            {
                FriendlyId = friendlyId,
                PatientId = patient.Id,
                DiseaseId = disease.Id,
                Type = CaseType.Case,
                DateOfOnset = dateOfOnset,
                DateOfNotification = dateOfNotification,
                ClinicalNotificationDate = clinicalNotificationDate,
                ClinicalNotifierOrganisation = clinicalNotificationDate.HasValue ? "Test Hospital" : null,
                ConfirmationStatusId = confirmationStatus?.Id,
                Hospitalised = hospitalized,
                HospitalId = hospital?.Id,
                DateOfAdmission = admissionDate,
                DateOfDischarge = dischargeDate,
                DiedDueToDisease = died,
                Jurisdiction1Id = patient.Jurisdiction1Id,
                Jurisdiction2Id = patient.Jurisdiction2Id,
                Jurisdiction3Id = patient.Jurisdiction3Id,
                Jurisdiction4Id = patient.Jurisdiction4Id,
                Jurisdiction5Id = patient.Jurisdiction5Id
            };

            return caseEntity;
        }

        private async Task<string> GenerateUniqueCaseIdAsync(int year)
        {
            string yearPrefix = $"C-{year}-";

            var existingIds = await _context.Cases
                .Where(c => c.FriendlyId.StartsWith(yearPrefix))
                .Select(c => c.FriendlyId)
                .ToListAsync();

            int maxSequence = 0;

            foreach (var id in existingIds)
            {
                var parts = id.Split('-');
                if (parts.Length == 3 && int.TryParse(parts[2], out int sequence))
                {
                    if (sequence > maxSequence)
                    {
                        maxSequence = sequence;
                    }
                }
            }

            int nextSequence = maxSequence + 1;
            return $"C-{year}-{nextSequence:D4}";
        }

        private DateTime GenerateRandomDateInYear(int year, Disease disease, bool useSeasonalPatterns)
        {
            if (!useSeasonalPatterns)
            {
                var dayOfYear = _random.Next(1, 366);
                return new DateTime(year, 1, 1).AddDays(dayOfYear - 1);
            }

            var diseaseName = disease.Name?.ToLower() ?? "";
            int month;

            if (diseaseName.Contains("flu") || diseaseName.Contains("influenza") || diseaseName.Contains("respiratory"))
            {
                month = _random.Next(0, 100) < 70
                    ? _random.Next(6, 9)
                    : _random.Next(1, 13);
            }
            else if (diseaseName.Contains("gastro") || diseaseName.Contains("salmonella") || diseaseName.Contains("listeria"))
            {
                month = _random.Next(0, 100) < 70
                    ? _random.Next(11, 13) == 13 ? _random.Next(1, 3) : _random.Next(11, 13)
                    : _random.Next(1, 13);
            }
            else
            {
                month = _random.Next(1, 13);
            }

            var daysInMonth = DateTime.DaysInMonth(year, month);
            var day = _random.Next(1, daysInMonth + 1);

            return new DateTime(year, month, day);
        }

        private async Task<LabResult> GenerateLabResultForCaseAsync(Case caseEntity, DateTime dateOfOnset)
        {
            var specimenDate = dateOfOnset.AddDays(_random.Next(-2, 5));
            var receivedDate = specimenDate.AddDays(_random.Next(0, 3));
            var testedDate = receivedDate.AddDays(_random.Next(0, 5));

            var nextSequence = await _context.LabResults.CountAsync() + 1;
            var friendlyId = $"LAB-{DateTime.UtcNow.Year}-{nextSequence:D5}";

            return new LabResult
            {
                FriendlyId = friendlyId,
                CaseId = caseEntity.Id,
                SpecimenCollectionDate = specimenDate,
                ResultDate = testedDate,
                Notes = "Auto-generated test lab result"
            };
        }

        private async Task GenerateCustomFieldValuesForCaseAsync(Guid caseId, Guid diseaseId)
        {
            var customFields = await _context.DiseaseCustomFields
                .Where(dcf => dcf.DiseaseId == diseaseId)
                .Include(dcf => dcf.CustomFieldDefinition)
                    .ThenInclude(cf => cf.LookupTable)
                        .ThenInclude(lt => lt.Values)
                .Select(dcf => dcf.CustomFieldDefinition)
                .Where(cf => cf.IsActive && cf.ShowOnCaseForm)
                .ToListAsync();

            foreach (var field in customFields)
            {
                try
                {
                    switch (field.FieldType)
                    {
                        case CustomFieldType.Text:
                        case CustomFieldType.TextArea:
                            _context.CaseCustomFieldStrings.Add(new CaseCustomFieldString
                            {
                                CaseId = caseId,
                                FieldDefinitionId = field.Id,
                                Value = $"Test value for {field.Label}",
                                UpdatedAt = DateTime.UtcNow
                            });
                            break;

                        case CustomFieldType.Number:
                            _context.CaseCustomFieldNumbers.Add(new CaseCustomFieldNumber
                            {
                                CaseId = caseId,
                                FieldDefinitionId = field.Id,
                                Value = _random.Next(1, 100),
                                UpdatedAt = DateTime.UtcNow
                            });
                            break;

                        case CustomFieldType.Date:
                            _context.CaseCustomFieldDates.Add(new CaseCustomFieldDate
                            {
                                CaseId = caseId,
                                FieldDefinitionId = field.Id,
                                Value = DateTime.UtcNow.AddDays(-_random.Next(0, 365)),
                                UpdatedAt = DateTime.UtcNow
                            });
                            break;

                        case CustomFieldType.Checkbox:
                            _context.CaseCustomFieldBooleans.Add(new CaseCustomFieldBoolean
                            {
                                CaseId = caseId,
                                FieldDefinitionId = field.Id,
                                Value = _random.Next(0, 2) == 1,
                                UpdatedAt = DateTime.UtcNow
                            });
                            break;

                        case CustomFieldType.Dropdown:
                            if (field.LookupTable?.Values != null && field.LookupTable.Values.Any())
                            {
                                var lookupValue = field.LookupTable.Values.ElementAt(_random.Next(field.LookupTable.Values.Count));
                                _context.CaseCustomFieldLookups.Add(new CaseCustomFieldLookup
                                {
                                    CaseId = caseId,
                                    FieldDefinitionId = field.Id,
                                    LookupValueId = lookupValue.Id,
                                    UpdatedAt = DateTime.UtcNow
                                });
                            }
                            break;
                    }
                }
                catch
                {
                }
            }
        }
    }

    public class TestDataGenerationResult
    {
        public int PatientsCreated { get; set; }
        public int CasesCreated { get; set; }
        public int LabResultsCreated { get; set; }
        public int CustomFieldsCreated { get; set; }
        public List<string> Errors { get; set; } = new();
        public DateTime StartTime { get; set; } = DateTime.UtcNow;
        public DateTime? EndTime { get; set; }

        public TimeSpan Duration => EndTime.HasValue 
            ? EndTime.Value - StartTime 
            : DateTime.UtcNow - StartTime;

        public bool HasErrors => Errors.Any();
    }
}
