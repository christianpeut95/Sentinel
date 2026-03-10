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
        private readonly ICaseIdGeneratorService _caseIdGenerator;
        private readonly Random _random = new Random();
        
        // Service-level lookup cache to avoid reloading on every call
        private LookupDataCache? _cachedLookups;

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
            IPatientIdGeneratorService patientIdGenerator,
            ICaseIdGeneratorService caseIdGenerator)
        {
            _context = context;
            _geocodingService = geocodingService;
            _jurisdictionService = jurisdictionService;
            _configuration = configuration;
            _patientIdGenerator = patientIdGenerator;
            _caseIdGenerator = caseIdGenerator;
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
        
        // Memory management helpers
        private void ClearContextMemory()
        {
            _context.ChangeTracker.Clear();
            GC.Collect();
            GC.WaitForPendingFinalizers();
        }
        
        // Streaming patient enumerator to avoid loading all patients into memory
        private async IAsyncEnumerable<Patient> StreamPatientsAsync(int count)
        {
            const int batchSize = 100;
            int skip = 0;
            
            while (skip < count)
            {
                var batch = await _context.Patients
                    .AsNoTracking()
                    .OrderBy(p => p.Id)
                    .Skip(skip)
                    .Take(Math.Min(batchSize, count - skip))
                    .ToListAsync();
                
                foreach (var patient in batch)
                {
                    yield return patient;
                }
                
                skip += batch.Count;
                if (batch.Count < batchSize) break;
            }
        }

        public async Task<TestDataGenerationResult> GenerateCasesAsync(
            int startYear,
            int endYear,
            int casesPerYear,
            List<Guid>? diseaseIds = null,
            CaseGenerationOptions? options = null,
            Action<string>? progressCallback = null)
        {
            options ??= new CaseGenerationOptions();
            var result = new TestDataGenerationResult();

            progressCallback?.Invoke("Loading diseases and lookup data...");

            var diseases = diseaseIds != null && diseaseIds.Any()
                ? await _context.Diseases.AsNoTracking().Where(d => diseaseIds.Contains(d.Id) && d.IsActive).ToListAsync()
                : await _context.Diseases.AsNoTracking().Where(d => d.IsActive).ToListAsync();

            if (!diseases.Any())
            {
                result.Errors.Add("No active diseases found");
                return result;
            }

            // PRE-LOAD ALL LOOKUPS ONCE (huge performance boost)
            var lookupCache = await LoadAllLookupsOnceAsync(diseases.Select(d => d.Id).ToList());

            progressCallback?.Invoke($"Generating cases from {startYear} to {endYear}...");

            int totalCasesNeeded = casesPerYear * (endYear - startYear + 1);

            // Check if we have enough patients
            var patientCount = await _context.Patients.CountAsync();
            if (patientCount < totalCasesNeeded)
            {
                result.Errors.Add($"Not enough patients. Need {totalCasesNeeded}, have {patientCount}. Generate more patients first.");
                return result;
            }

            // STREAM patients instead of loading all at once
            var patientStream = StreamPatientsAsync(totalCasesNeeded);
            var patientEnumerator = patientStream.GetAsyncEnumerator();

            const int BATCH_SIZE = 20; // Process 20 cases at a time
            int casesInCurrentBatch = 0;

            for (int year = startYear; year <= endYear; year++)
            {
                for (int i = 0; i < casesPerYear; i++)
                {
                    try
                    {
                        // Get next patient from stream
                        if (!await patientEnumerator.MoveNextAsync())
                        {
                            result.Errors.Add($"Ran out of patients at year {year}, case {i}");
                            await _context.SaveChangesAsync();
                            return result;
                        }

                        var patient = patientEnumerator.Current;
                        var disease = GetRandom(diseases.ToArray());
                        var dateOfOnset = GenerateRandomDateInYear(year, disease, options.UseSeasonalPatterns);

                        // Generate case and related records using CACHED lookups
                        var caseEntity = GenerateSingleCaseFromCache(patient, disease, dateOfOnset, lookupCache);
                        _context.Cases.Add(caseEntity);
                        result.CasesCreated++;
                        casesInCurrentBatch++;

                        // Generate lab results from cache
                        if (options.IncludeLabResults && _random.Next(0, 100) < options.LabResultProbabilityPercent)
                        {
                            int labCount = _random.Next(options.LabResultsPerCaseMin, options.LabResultsPerCaseMax + 1);
                            for (int l = 0; l < labCount; l++)
                            {
                                var labResult = GenerateLabResultFromCache(caseEntity, dateOfOnset, disease, lookupCache);
                                _context.LabResults.Add(labResult);
                                result.LabResultsCreated++;
                            }
                        }

                        // Generate symptoms from cache
                        if (options.IncludeSymptoms && _random.Next(0, 100) < options.SymptomProbabilityPercent)
                        {
                            GenerateSymptomsFromCache(caseEntity.Id, disease.Id, dateOfOnset, lookupCache);
                            result.SymptomsCreated += _context.ChangeTracker.Entries<CaseSymptom>()
                                .Count(e => e.State == EntityState.Added);
                        }

                        // Generate notes
                        if (options.IncludeNotes && _random.Next(0, 100) < options.CaseNoteProbabilityPercent)
                        {
                            GenerateNotesForCase(caseEntity.Id, patient.Id);
                            result.NotesCreated += _context.ChangeTracker.Entries<Note>()
                                .Count(e => e.State == EntityState.Added);
                        }

                        // Generate custom field values
                        if (options.IncludeCustomFields && _random.Next(0, 100) < options.CustomFieldProbabilityPercent)
                        {
                            await GenerateCustomFieldValuesForCaseAsync(caseEntity.Id, disease.Id);
                            result.CustomFieldsCreated += _context.ChangeTracker.Entries()
                                .Count(e => (e.State == EntityState.Added) &&
                                           (e.Entity is CaseCustomFieldString ||
                                            e.Entity is CaseCustomFieldNumber ||
                                            e.Entity is CaseCustomFieldDate ||
                                            e.Entity is CaseCustomFieldBoolean ||
                                            e.Entity is CaseCustomFieldLookup));
                        }

                        // Save and clear every BATCH_SIZE cases
                        if (casesInCurrentBatch >= BATCH_SIZE)
                        {
                            await _context.SaveChangesAsync();
                            ClearContextMemory(); // CRITICAL: Release memory
                            casesInCurrentBatch = 0;

                            progressCallback?.Invoke($"Created {result.CasesCreated}/{totalCasesNeeded} cases ({year})...");
                        }
                    }
                    catch (Exception ex)
                    {
                        result.Errors.Add($"Year {year}, Case {i + 1}: {ex.Message}");
                    }
                }

                // Save any remaining cases in batch
                if (casesInCurrentBatch > 0)
                {
                    await _context.SaveChangesAsync();
                    ClearContextMemory();
                    casesInCurrentBatch = 0;
                }

                progressCallback?.Invoke($"? Completed {year}: {casesPerYear} cases");
            }

            await patientEnumerator.DisposeAsync();

            progressCallback?.Invoke($"? Successfully created {result.CasesCreated} cases across {endYear - startYear + 1} years");
            return result;
        }

        // Load all lookups once and cache them (called once per generation session)
        private async Task<LookupDataCache> LoadAllLookupsOnceAsync(List<Guid> diseaseIds)
        {
            if (_cachedLookups != null) return _cachedLookups;

            var cache = new LookupDataCache();

            // Load all lookups SEQUENTIALLY with AsNoTracking for performance
            // (DbContext is NOT thread-safe, cannot use Task.WhenAll with same context)
            cache.SpecimenTypes = await _context.SpecimenTypes
                .AsNoTracking()
                .Where(st => st.IsActive)
                .ToListAsync();

            cache.TestTypes = await _context.TestTypes
                .AsNoTracking()
                .Where(tt => tt.IsActive)
                .ToListAsync();

            cache.TestResults = await _context.TestResults
                .AsNoTracking()
                .Where(tr => tr.IsActive)
                .ToListAsync();

            cache.ResultUnits = await _context.ResultUnits
                .AsNoTracking()
                .ToListAsync();

            cache.Laboratories = await _context.Organizations
                .AsNoTracking()
                .Include(o => o.OrganizationType)
                .Where(o => o.IsActive && o.OrganizationType!.Name.Contains("Lab"))
                .ToListAsync();

            cache.Providers = await _context.Organizations
                .AsNoTracking()
                .Include(o => o.OrganizationType)
                .Where(o => o.IsActive && o.OrganizationType!.Name.Contains("Provider"))
                .ToListAsync();

            cache.CaseStatuses = await _context.CaseStatuses
                .AsNoTracking()
                .Where(cs => cs.IsActive)
                .ToListAsync();

            cache.ContactClassifications = await _context.ContactClassifications
                .AsNoTracking()
                .Where(cc => cc.IsActive)
                .ToListAsync();

            cache.Jurisdictions = await _context.Jurisdictions
                .AsNoTracking()
                .Where(j => j.IsActive)
                .ToListAsync();

            // Load disease symptoms for ALL selected diseases at once
            var symptoms = await _context.DiseaseSymptoms
                .AsNoTracking()
                .Where(ds => diseaseIds.Contains(ds.DiseaseId) && !ds.IsDeleted)
                .Include(ds => ds.Symptom)
                .ToListAsync();

            cache.DiseaseSymptoms = symptoms
                .GroupBy(ds => ds.DiseaseId)
                .ToDictionary(g => g.Key, g => g.ToList());

            _cachedLookups = cache;
            return cache;
        }

        // Generate case using cached lookups (NO database queries)
        private Case GenerateSingleCaseFromCache(
            Patient patient,
            Disease disease,
            DateTime dateOfOnset,
            LookupDataCache cache)
        {
            var dateOfNotification = dateOfOnset.AddDays(_random.Next(1, 8));
            var clinicalNotificationDate = _random.Next(0, 100) < 40
                ? dateOfOnset.AddDays(_random.Next(0, 4))
                : (DateTime?)null;

            var confirmationStatus = cache.CaseStatuses.Any()
                ? cache.CaseStatuses[_random.Next(cache.CaseStatuses.Count)]
                : null;

            var hospitalized = (YesNoUnknown)_random.Next(0, 3);

            return new Case
            {
                // FriendlyId will be generated by DbContext on save
                PatientId = patient.Id,
                DiseaseId = disease.Id,
                Type = CaseType.Case,
                DateOfOnset = dateOfOnset,
                DateOfNotification = dateOfNotification,
                ClinicalNotificationDate = clinicalNotificationDate,
                ClinicalNotifierOrganisation = clinicalNotificationDate.HasValue ? "Test Hospital" : null,
                ConfirmationStatusId = confirmationStatus?.Id,
                Hospitalised = hospitalized,
                Jurisdiction1Id = patient.Jurisdiction1Id,
                Jurisdiction2Id = patient.Jurisdiction2Id,
                Jurisdiction3Id = patient.Jurisdiction3Id,
                Jurisdiction4Id = patient.Jurisdiction4Id,
                Jurisdiction5Id = patient.Jurisdiction5Id
            };
        }

        // Generate lab result using cached lookups (NO database queries)
        private LabResult GenerateLabResultFromCache(
            Case caseEntity,
            DateTime dateOfOnset,
            Disease disease,
            LookupDataCache cache)
        {
            var specimenDate = dateOfOnset.AddDays(_random.Next(-2, 5));
            var receivedDate = specimenDate.AddDays(_random.Next(0, 3));
            var resultDate = receivedDate.AddDays(_random.Next(1, 7));

            // Prefer Positive/Detected results (80% chance) for testing purposes
            TestResult? testResult = null;
            if (cache.TestResults.Any())
            {
                if (_random.Next(0, 100) < 80)
                {
                    testResult = cache.TestResults
                        .FirstOrDefault(tr => tr.Name.Contains("Positive") || tr.Name.Contains("Detected"))
                        ?? cache.TestResults[_random.Next(cache.TestResults.Count)];
                }
                else
                {
                    testResult = cache.TestResults[_random.Next(cache.TestResults.Count)];
                }
            }

            // Quantitative result (40% chance)
            decimal? quantResult = null;
            int? unitsId = null;
            if (_random.Next(0, 100) < 40 && cache.ResultUnits.Any())
            {
                quantResult = (decimal)(_random.NextDouble() * 100);
                unitsId = cache.ResultUnits[_random.Next(cache.ResultUnits.Count)].Id;
            }

            return new LabResult
            {
                CaseId = caseEntity.Id,
                LaboratoryId = cache.Laboratories.Any() ? cache.Laboratories[_random.Next(cache.Laboratories.Count)].Id : null,
                OrderingProviderId = cache.Providers.Any() ? cache.Providers[_random.Next(cache.Providers.Count)].Id : null,
                AccessionNumber = $"ACC-{_random.Next(100000, 999999)}",
                SpecimenCollectionDate = specimenDate,
                SpecimenTypeId = cache.SpecimenTypes.Any() ? cache.SpecimenTypes[_random.Next(cache.SpecimenTypes.Count)].Id : null,
                TestTypeId = cache.TestTypes.Any() ? cache.TestTypes[_random.Next(cache.TestTypes.Count)].Id : null,
                TestedDiseaseId = disease.Id,
                TestResultId = testResult?.Id,
                ResultDate = resultDate,
                QuantitativeResult = quantResult,
                ResultUnitsId = unitsId,
                Notes = _random.Next(0, 100) < 30 ? "Auto-generated test lab result" : null
            };
        }

        // Generate symptoms using cached lookups (NO database queries)
        private void GenerateSymptomsFromCache(
            Guid caseId,
            Guid diseaseId,
            DateTime dateOfOnset,
            LookupDataCache cache)
        {
            // Get from pre-loaded cache
            if (!cache.DiseaseSymptoms.TryGetValue(diseaseId, out var diseaseSymptoms) || !diseaseSymptoms.Any())
                return;

            var commonSymptoms = diseaseSymptoms.Where(ds => ds.IsCommon).ToList();
            var otherSymptoms = diseaseSymptoms.Where(ds => !ds.IsCommon).ToList();

            var symptomsToAdd = new List<DiseaseSymptom>();

            // Add 1-3 common symptoms (80% chance)
            if (commonSymptoms.Any())
            {
                var count = _random.Next(1, Math.Min(4, commonSymptoms.Count + 1));
                symptomsToAdd.AddRange(commonSymptoms.OrderBy(x => Guid.NewGuid()).Take(count));
            }

            // Add 0-2 other symptoms (40% chance)
            if (otherSymptoms.Any() && _random.Next(0, 100) < 40)
            {
                var count = _random.Next(0, Math.Min(3, otherSymptoms.Count + 1));
                symptomsToAdd.AddRange(otherSymptoms.OrderBy(x => Guid.NewGuid()).Take(count));
            }

            foreach (var ds in symptomsToAdd.Distinct())
            {
                var onsetDate = dateOfOnset.AddDays(_random.Next(-3, 3));

                _context.CaseSymptoms.Add(new CaseSymptom
                {
                    CaseId = caseId,
                    SymptomId = ds.SymptomId,
                    OnsetDate = onsetDate,
                    Severity = GetRandomSeverity(),
                    Notes = _random.Next(0, 100) < 20 ? "Test symptom note" : null
                });
            }
        }
        
        private string GetRandomSeverity()
        {
            var severities = new[] { "Mild", "Moderate", "Severe" };
            return severities[_random.Next(severities.Length)];
        }

        // Generate notes for case (NO database queries)
        private void GenerateNotesForCase(Guid caseId, Guid patientId)
        {
            var noteCount = _random.Next(0, 4); // 0-3 notes

            var templates = new[]
            {
                "Initial contact made via phone",
                "Patient reported symptoms consistent with case definition",
                "Follow-up call scheduled",
                "Laboratory results reviewed",
                "Patient advised on isolation requirements",
                "Contact tracing completed"
            };

            for (int i = 0; i < noteCount; i++)
            {
                _context.Notes.Add(new Note
                {
                    CaseId = caseId,
                    PatientId = patientId,
                    Type = NoteType.Note,
                    Content = templates[_random.Next(templates.Length)],
                    CreatedAt = DateTime.UtcNow.AddDays(-_random.Next(0, 30))
                });
            }
        }

        // OLD METHOD - DEPRECATED - Keep for backwards compatibility but not used
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

            return new LabResult
            {
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
        
        /// <summary>
        /// DEMO ONLY: Deletes all test data (patients, cases, and related records) from the database.
        /// This method has multiple safeguards and only works in Demo mode.
        /// </summary>
        public async Task<TestDataDeletionResult> DeleteAllTestDataAsync(
            string confirmationCode,
            Action<string>? progressCallback = null)
        {
            var result = new TestDataDeletionResult();
            
            // SAFEGUARD 1: Check if we're in Demo mode
            var isDemoMode = _configuration.GetValue<bool>("Demo:EnableDemoMode");
            if (!isDemoMode)
            {
                result.Errors.Add("? BLOCKED: This function is only available in Demo mode.");
                result.Errors.Add("Set 'Demo:EnableDemoMode' to true in appsettings.json");
                return result;
            }
            
            // SAFEGUARD 2: Require confirmation code
            const string EXPECTED_CODE = "DELETE-ALL-DEMO-DATA";
            if (confirmationCode != EXPECTED_CODE)
            {
                result.Errors.Add($"? BLOCKED: Invalid confirmation code.");
                result.Errors.Add($"Expected: {EXPECTED_CODE}");
                return result;
            }
            
            var connectionString = _configuration.GetConnectionString("DefaultConnection") ?? "";
            var databaseName = connectionString.Contains("Database=") 
                ? connectionString.Split("Database=")[1].Split(';')[0] 
                : "Unknown";
            
            progressCallback?.Invoke("?? WARNING: Starting deletion of ALL test data...");
            progressCallback?.Invoke($"Database: {databaseName}");
            
            try
            {
                // Delete in reverse dependency order to avoid foreign key violations
                
                progressCallback?.Invoke("Deleting review queue entries...");
                var reviewQueueCount = await _context.ReviewQueue.IgnoreQueryFilters().CountAsync();
                if (reviewQueueCount > 0)
                {
                    _context.ReviewQueue.RemoveRange(await _context.ReviewQueue.IgnoreQueryFilters().ToListAsync());
                    await _context.SaveChangesAsync();
                    result.ReviewQueueEntriesDeleted = reviewQueueCount;
                }
                
                progressCallback?.Invoke("Deleting case tasks...");
                var tasksCount = await _context.CaseTasks.CountAsync();
                if (tasksCount > 0)
                {
                    _context.CaseTasks.RemoveRange(await _context.CaseTasks.ToListAsync());
                    await _context.SaveChangesAsync();
                    result.TasksDeleted = tasksCount;
                }
                
                progressCallback?.Invoke("Deleting exposure events...");
                var exposuresCount = await _context.ExposureEvents.IgnoreQueryFilters().CountAsync();
                if (exposuresCount > 0)
                {
                    _context.ExposureEvents.RemoveRange(await _context.ExposureEvents.IgnoreQueryFilters().ToListAsync());
                    await _context.SaveChangesAsync();
                    result.ExposuresDeleted = exposuresCount;
                }
                
                progressCallback?.Invoke("Deleting case symptoms...");
                var symptomsCount = await _context.CaseSymptoms.IgnoreQueryFilters().CountAsync();
                if (symptomsCount > 0)
                {
                    _context.CaseSymptoms.RemoveRange(await _context.CaseSymptoms.IgnoreQueryFilters().ToListAsync());
                    await _context.SaveChangesAsync();
                    result.SymptomsDeleted = symptomsCount;
                }
                
                progressCallback?.Invoke("Deleting notes...");
                var notesCount = await _context.Notes.IgnoreQueryFilters().CountAsync();
                if (notesCount > 0)
                {
                    _context.Notes.RemoveRange(await _context.Notes.IgnoreQueryFilters().ToListAsync());
                    await _context.SaveChangesAsync();
                    result.NotesDeleted = notesCount;
                }
                
                progressCallback?.Invoke("Deleting case custom fields...");
                var caseStringsCount = await _context.CaseCustomFieldStrings.CountAsync();
                var caseNumbersCount = await _context.CaseCustomFieldNumbers.CountAsync();
                var caseDatesCount = await _context.CaseCustomFieldDates.CountAsync();
                var caseBooleansCount = await _context.CaseCustomFieldBooleans.CountAsync();
                var caseLookupsCount = await _context.CaseCustomFieldLookups.CountAsync();
                
                if (caseStringsCount > 0) _context.CaseCustomFieldStrings.RemoveRange(await _context.CaseCustomFieldStrings.ToListAsync());
                if (caseNumbersCount > 0) _context.CaseCustomFieldNumbers.RemoveRange(await _context.CaseCustomFieldNumbers.ToListAsync());
                if (caseDatesCount > 0) _context.CaseCustomFieldDates.RemoveRange(await _context.CaseCustomFieldDates.ToListAsync());
                if (caseBooleansCount > 0) _context.CaseCustomFieldBooleans.RemoveRange(await _context.CaseCustomFieldBooleans.ToListAsync());
                if (caseLookupsCount > 0) _context.CaseCustomFieldLookups.RemoveRange(await _context.CaseCustomFieldLookups.ToListAsync());
                
                await _context.SaveChangesAsync();
                result.CaseCustomFieldsDeleted = caseStringsCount + caseNumbersCount + caseDatesCount + caseBooleansCount + caseLookupsCount;
                
                progressCallback?.Invoke("Deleting lab results...");
                var labResultsCount = await _context.LabResults.IgnoreQueryFilters().CountAsync();
                if (labResultsCount > 0)
                {
                    _context.LabResults.RemoveRange(await _context.LabResults.IgnoreQueryFilters().ToListAsync());
                    await _context.SaveChangesAsync();
                    result.LabResultsDeleted = labResultsCount;
                }
                
                progressCallback?.Invoke("Deleting outbreak cases...");
                var outbreakCasesCount = await _context.OutbreakCases.CountAsync();
                if (outbreakCasesCount > 0)
                {
                    _context.OutbreakCases.RemoveRange(await _context.OutbreakCases.ToListAsync());
                    await _context.SaveChangesAsync();
                }
                
                progressCallback?.Invoke("Deleting cases...");
                var casesCount = await _context.Cases.IgnoreQueryFilters().CountAsync();
                if (casesCount > 0)
                {
                    _context.Cases.RemoveRange(await _context.Cases.IgnoreQueryFilters().ToListAsync());
                    await _context.SaveChangesAsync();
                    result.CasesDeleted = casesCount;
                }
                
                progressCallback?.Invoke("Deleting patient custom fields...");
                var patientStringsCount = await _context.PatientCustomFieldStrings.CountAsync();
                var patientNumbersCount = await _context.PatientCustomFieldNumbers.CountAsync();
                var patientDatesCount = await _context.PatientCustomFieldDates.CountAsync();
                var patientBooleansCount = await _context.PatientCustomFieldBooleans.CountAsync();
                var patientLookupsCount = await _context.PatientCustomFieldLookups.CountAsync();
                
                if (patientStringsCount > 0) _context.PatientCustomFieldStrings.RemoveRange(await _context.PatientCustomFieldStrings.ToListAsync());
                if (patientNumbersCount > 0) _context.PatientCustomFieldNumbers.RemoveRange(await _context.PatientCustomFieldNumbers.ToListAsync());
                if (patientDatesCount > 0) _context.PatientCustomFieldDates.RemoveRange(await _context.PatientCustomFieldDates.ToListAsync());
                if (patientBooleansCount > 0) _context.PatientCustomFieldBooleans.RemoveRange(await _context.PatientCustomFieldBooleans.ToListAsync());
                if (patientLookupsCount > 0) _context.PatientCustomFieldLookups.RemoveRange(await _context.PatientCustomFieldLookups.ToListAsync());
                
                await _context.SaveChangesAsync();
                result.PatientCustomFieldsDeleted = patientStringsCount + patientNumbersCount + patientDatesCount + patientBooleansCount + patientLookupsCount;
                
                progressCallback?.Invoke("Deleting patients...");
                var patientsCount = await _context.Patients.IgnoreQueryFilters().CountAsync();
                if (patientsCount > 0)
                {
                    _context.Patients.RemoveRange(await _context.Patients.IgnoreQueryFilters().ToListAsync());
                    await _context.SaveChangesAsync();
                    result.PatientsDeleted = patientsCount;
                }
                
                progressCallback?.Invoke("Clearing audit logs for deleted entities...");
                var auditLogsCount = await _context.AuditLogs
                    .Where(a => a.EntityType == "Patient" || a.EntityType == "Case" || a.EntityType == "LabResult")
                    .CountAsync();
                if (auditLogsCount > 0)
                {
                    var auditLogs = await _context.AuditLogs
                        .Where(a => a.EntityType == "Patient" || a.EntityType == "Case" || a.EntityType == "LabResult")
                        .ToListAsync();
                    _context.AuditLogs.RemoveRange(auditLogs);
                    await _context.SaveChangesAsync();
                    result.AuditLogsDeleted = auditLogsCount;
                }
                
                // Clear cache
                _cachedLookups = null;
                ClearContextMemory();
                
                result.EndTime = DateTime.UtcNow;
                progressCallback?.Invoke("");
                progressCallback?.Invoke("? ============================================");
                progressCallback?.Invoke($"? DELETION COMPLETE in {result.Duration.TotalSeconds:F1}s");
                progressCallback?.Invoke("? ============================================");
                progressCallback?.Invoke($"   Patients deleted:           {result.PatientsDeleted:N0}");
                progressCallback?.Invoke($"   Cases deleted:              {result.CasesDeleted:N0}");
                progressCallback?.Invoke($"   Lab results deleted:        {result.LabResultsDeleted:N0}");
                progressCallback?.Invoke($"   Symptoms deleted:           {result.SymptomsDeleted:N0}");
                progressCallback?.Invoke($"   Notes deleted:              {result.NotesDeleted:N0}");
                progressCallback?.Invoke($"   Exposures deleted:          {result.ExposuresDeleted:N0}");
                progressCallback?.Invoke($"   Tasks deleted:              {result.TasksDeleted:N0}");
                progressCallback?.Invoke($"   Patient custom fields:      {result.PatientCustomFieldsDeleted:N0}");
                progressCallback?.Invoke($"   Case custom fields:         {result.CaseCustomFieldsDeleted:N0}");
                progressCallback?.Invoke($"   Review queue entries:       {result.ReviewQueueEntriesDeleted:N0}");
                progressCallback?.Invoke($"   Audit logs cleaned:         {result.AuditLogsDeleted:N0}");
                progressCallback?.Invoke("? ============================================");
                
                return result;
            }
            catch (Exception ex)
            {
                result.Errors.Add($"? Error during deletion: {ex.Message}");
                if (ex.InnerException != null)
                {
                    result.Errors.Add($"Inner exception: {ex.InnerException.Message}");
                }
                return result;
            }
        }
    }

    // Lookup data cache to avoid repeated database queries
    internal class LookupDataCache
    {
        // Lab Result Lookups
        public List<SpecimenType> SpecimenTypes { get; set; } = new();
        public List<TestType> TestTypes { get; set; } = new();
        public List<TestResult> TestResults { get; set; } = new();
        public List<ResultUnits> ResultUnits { get; set; } = new();
        public List<Organization> Laboratories { get; set; } = new();
        public List<Organization> Providers { get; set; } = new();

        // Case Lookups
        public List<CaseStatus> CaseStatuses { get; set; } = new();
        public List<Jurisdiction> Jurisdictions { get; set; } = new();
        public List<ContactClassification> ContactClassifications { get; set; } = new();

        // Disease-specific (keyed by DiseaseId)
        public Dictionary<Guid, List<DiseaseSymptom>> DiseaseSymptoms { get; set; } = new();
    }

    // Options for case generation
    public class CaseGenerationOptions
    {
        public bool IncludeLabResults { get; set; } = true;
        public int LabResultsPerCaseMin { get; set; } = 1;
        public int LabResultsPerCaseMax { get; set; } = 3;
        public int LabResultProbabilityPercent { get; set; } = 80; // 80% of cases get lab results

        public bool IncludeSymptoms { get; set; } = true;
        public int SymptomProbabilityPercent { get; set; } = 85; // 85% of cases get symptoms

        public bool IncludeNotes { get; set; } = true;
        public int CaseNoteProbabilityPercent { get; set; } = 70; // 70% of cases get notes

        public bool IncludeCustomFields { get; set; } = true;
        public int CustomFieldProbabilityPercent { get; set; } = 75; // 75% of cases get custom fields

        public bool UseSeasonalPatterns { get; set; } = true;
    }

    public class TestDataGenerationResult
    {
        public int PatientsCreated { get; set; }
        public int CasesCreated { get; set; }
        public int LabResultsCreated { get; set; }
        public int CustomFieldsCreated { get; set; }
        public int SymptomsCreated { get; set; }
        public int NotesCreated { get; set; }
        public int ExposuresCreated { get; set; }
        public List<string> Errors { get; set; } = new();
        public DateTime StartTime { get; set; } = DateTime.UtcNow;
        public DateTime? EndTime { get; set; }

        public TimeSpan Duration => EndTime.HasValue 
            ? EndTime.Value - StartTime 
            : DateTime.UtcNow - StartTime;

        public bool HasErrors => Errors.Any();
    }
    
    // Result class for deletion operations
    public class TestDataDeletionResult
    {
        public int PatientsDeleted { get; set; }
        public int CasesDeleted { get; set; }
        public int LabResultsDeleted { get; set; }
        public int SymptomsDeleted { get; set; }
        public int NotesDeleted { get; set; }
        public int ExposuresDeleted { get; set; }
        public int TasksDeleted { get; set; }
        public int PatientCustomFieldsDeleted { get; set; }
        public int CaseCustomFieldsDeleted { get; set; }
        public int ReviewQueueEntriesDeleted { get; set; }
        public int AuditLogsDeleted { get; set; }
        public List<string> Errors { get; set; } = new();
        public DateTime StartTime { get; set; } = DateTime.UtcNow;
        public DateTime? EndTime { get; set; }

        public TimeSpan Duration => EndTime.HasValue 
            ? EndTime.Value - StartTime 
            : DateTime.UtcNow - StartTime;

        public bool HasErrors => Errors.Any();
        
        public int TotalDeleted => PatientsDeleted + CasesDeleted + LabResultsDeleted + 
                                   SymptomsDeleted + NotesDeleted + ExposuresDeleted + 
                                   TasksDeleted + PatientCustomFieldsDeleted + 
                                   CaseCustomFieldsDeleted + ReviewQueueEntriesDeleted + 
                                   AuditLogsDeleted;
    }
}
