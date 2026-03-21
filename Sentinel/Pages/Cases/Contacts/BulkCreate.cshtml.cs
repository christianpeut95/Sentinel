using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Sentinel.Data;
using Sentinel.DTOs;
using Sentinel.Models;
using Sentinel.Services;
using System.Globalization;
using System.Security.Claims;
using CsvHelper;
using CsvHelper.Configuration;

namespace Sentinel.Pages.Cases.Contacts;

[Authorize(Policy = "Permission.Case.Create")]
public class BulkCreateModel : PageModel
{
    private readonly ApplicationDbContext _context;
    private readonly IDuplicateDetectionService _duplicateDetectionService;
    private readonly IPatientIdGeneratorService _patientIdGenerator;
    private readonly ICaseIdGeneratorService _caseIdGenerator;
    private readonly IOutbreakService _outbreakService;

    public BulkCreateModel(
        ApplicationDbContext context,
        IDuplicateDetectionService duplicateDetectionService,
        IPatientIdGeneratorService patientIdGenerator,
        ICaseIdGeneratorService caseIdGenerator,
        IOutbreakService outbreakService)
    {
        _context = context;
        _duplicateDetectionService = duplicateDetectionService;
        _patientIdGenerator = patientIdGenerator;
        _caseIdGenerator = caseIdGenerator;
        _outbreakService = outbreakService;
    }

    // Route parameters
    [BindProperty(SupportsGet = true)]
    public Guid CaseId { get; set; }

    [BindProperty(SupportsGet = true)]
    public Guid? LocationId { get; set; }

    [BindProperty(SupportsGet = true)]
    public Guid? EventId { get; set; }

    [BindProperty(SupportsGet = true)]
    public DateTime? ExposureStartDate { get; set; }

    [BindProperty(SupportsGet = true)]
    public DateTime? ExposureEndDate { get; set; }

    [BindProperty(SupportsGet = true)]
    public int? OutbreakId { get; set; }

    // Page data
    public Case SourceCase { get; set; } = default!;
    public Location? Location { get; set; }
    public Event? Event { get; set; }
    public SelectList ContactClassificationsList { get; set; } = default!;
    public SelectList ExposureStatusList { get; set; } = default!;
    public SelectList OutbreaksList { get; set; } = default!;


    // For Step 2: Review screen
    [BindProperty]
    public List<BulkContactDto> ContactList { get; set; } = new();

    [BindProperty]
    public bool ShowReviewScreen { get; set; }

    public List<DuplicateDetectionResult> DuplicateResults { get; set; } = new();

    [TempData]
    public string? SuccessMessage { get; set; }

    [TempData]
    public string? ErrorMessage { get; set; }

    public async Task<IActionResult> OnGetAsync()
    {
        await LoadPageDataAsync();

        if (SourceCase == null)
            return NotFound();

        return Page();
    }

    private async Task LoadPageDataAsync()
    {
        // Load source case
        SourceCase = await _context.Cases
            .Include(c => c.Patient)
            .Include(c => c.Disease)
            .FirstOrDefaultAsync(c => c.Id == CaseId);

        if (SourceCase == null)
            return;

        // Load location if specified
        if (LocationId.HasValue)
        {
            Location = await _context.Locations
                .Include(l => l.LocationType)
                .FirstOrDefaultAsync(l => l.Id == LocationId.Value);
        }

        // Load event if specified
        if (EventId.HasValue)
        {
            Event = await _context.Events
                .Include(e => e.Location)
                .Include(e => e.EventType)
                .FirstOrDefaultAsync(e => e.Id == EventId.Value);
        }

        await LoadContactClassifications();
        LoadExposureStatuses();
        await LoadOutbreaksAsync();
    }


    public async Task<IActionResult> OnPostUploadCsvAsync(IFormFile csvFile)
    {
        if (csvFile == null || csvFile.Length == 0)
        {
            ErrorMessage = "Please select a CSV file to upload.";
            return RedirectToPage(new { CaseId, LocationId, EventId, ExposureStartDate, ExposureEndDate, OutbreakId });
        }

        try
        {
            var contacts = new List<BulkContactDto>();

            using (var reader = new StreamReader(csvFile.OpenReadStream()))
            using (var csv = new CsvReader(reader, new CsvConfiguration(CultureInfo.InvariantCulture)
            {
                HeaderValidated = null,
                MissingFieldFound = null
            }))
            {
                var records = csv.GetRecords<BulkContactCsvRow>();
                int rowNum = 1;

                foreach (var record in records)
                {
                    contacts.Add(new BulkContactDto
                    {
                        RowNumber = rowNum++,
                        FirstName = record.FirstName ?? string.Empty,
                        LastName = record.LastName ?? string.Empty,
                        DateOfBirth = ParseDate(record.DateOfBirth),
                        ContactPhone = record.Phone,
                        Email = record.Email,
                        ParentGuardianName = record.ParentGuardianName,
                        ParentGuardianPhone = record.ParentGuardianPhone,
                        ExposureStartDate = ExposureStartDate ?? DateTime.Today,
                        ExposureEndDate = ExposureEndDate,
                        ExposureStatus = record.ExposureStatus ?? "ConfirmedExposure",
                        ConfidenceLevel = record.ConfidenceLevel,
                        Notes = record.Notes,
                        IncludeInImport = true // Default to included
                    });
                }

            }

            // Run duplicate detection
            DuplicateResults = await _duplicateDetectionService.AnalyzeBulkContactsAsync(contacts);

            // Update contacts with duplicate flags
            foreach (var result in DuplicateResults)
            {
                var contact = contacts[result.RowNumber - 1];
                contact.IsPotentialDuplicate = result.IsPotentialDuplicate;
                contact.PossibleMatchPatientIds = result.PossibleMatches.Select(m => m.Id).ToList();
                contact.MatchReason = GetMatchReason(result);
            }

            ContactList = contacts;
            ShowReviewScreen = true;

            // Reload page data
            await LoadPageDataAsync();

            if (SourceCase == null)
                return NotFound();

            return Page();
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Error processing CSV file: {ex.Message}";
            return RedirectToPage(new { CaseId, LocationId, EventId, ExposureStartDate, ExposureEndDate, OutbreakId });
        }
    }

    public async Task<IActionResult> OnPostConfirmAsync()
    {
        // Load page data first (needed for SourceCase)
        await LoadPageDataAsync();

        if (SourceCase == null)
            return NotFound();

        if (!ContactList.Any())
        {
            ErrorMessage = "No contacts to create.";
            return RedirectToPage(new { CaseId, LocationId, EventId, ExposureStartDate, ExposureEndDate, OutbreakId });
        }

        // Filter to only included contacts
        var contactsToCreate = ContactList.Where(c => c.IncludeInImport).ToList();

        if (!contactsToCreate.Any())
        {
            ErrorMessage = "No contacts selected for import. Please check at least one contact to import.";
            ShowReviewScreen = true;
            await LoadPageDataAsync();
            return Page();
        }

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        int createdCount = 0;
        int linkedCount = 0;
        var createdContactIds = new List<Guid>();

        try
        {
            foreach (var contactDto in contactsToCreate)
            {
                Patient patient;

                // Check if user wants to link to existing patient
                if (contactDto.LinkToExistingPatientId.HasValue)
                {
                    patient = await _context.Patients.FindAsync(contactDto.LinkToExistingPatientId.Value);
                    if (patient == null)
                        continue;
                    linkedCount++;
                }
                else
                {
                    // Create new patient
                    patient = new Patient
                    {
                        Id = Guid.NewGuid(),
                        // FriendlyId will be auto-generated by DbContext.SaveChangesAsync()
                        GivenName = contactDto.FirstName,
                        FamilyName = contactDto.LastName,
                        DateOfBirth = contactDto.DateOfBirth,
                        MobilePhone = contactDto.ContactPhone,
                        EmailAddress = contactDto.Email,
                        CreatedAt = DateTime.UtcNow,
                        CreatedByUserId = userId
                    };

                    _context.Patients.Add(patient);
                    createdCount++;
                }

                // Create contact case
                var contact = new Case
                {
                    Id = Guid.NewGuid(),
                    // FriendlyId will be auto-generated by DbContext.SaveChangesAsync()
                    PatientId = patient.Id,
                    Type = CaseType.Contact,
                    DiseaseId = SourceCase.DiseaseId,
                    DateOfNotification = DateTime.Today
                };

                _context.Cases.Add(contact);
                createdContactIds.Add(contact.Id);

                // Parse exposure status from DTO
                var exposureStatus = contactDto.ExposureStatus switch
                {
                    "ConfirmedExposure" => ExposureStatus.ConfirmedExposure,
                    "PotentialExposure" => ExposureStatus.PotentialExposure,
                    "UnderInvestigation" => ExposureStatus.UnderInvestigation,
                    "RuledOut" => ExposureStatus.RuledOut,
                    _ => ExposureStatus.ConfirmedExposure
                };

                var exposureEvent = new ExposureEvent
                {
                    Id = Guid.NewGuid(),
                    SourceCaseId = CaseId,
                    ExposedCaseId = contact.Id,
                    LocationId = LocationId,
                    EventId = EventId,
                    ContactClassificationId = contactDto.ContactClassificationId,
                    ExposureStartDate = contactDto.ExposureStartDate,
                    ExposureEndDate = contactDto.ExposureEndDate,
                    ExposureType = ExposureType.Contact,
                    ExposureStatus = exposureStatus,
                    ConfidenceLevel = contactDto.ConfidenceLevel,
                    Description = contactDto.Notes
                };

                _context.ExposureEvents.Add(exposureEvent);
            }

            await _context.SaveChangesAsync();

            // Link all created contacts to the selected outbreak (if any)
            if (OutbreakId.HasValue && createdContactIds.Any())
            {
                foreach (var contactId in createdContactIds)
                {
                    await _outbreakService.LinkCaseAsync(
                        OutbreakId.Value,
                        contactId,
                        classification: null,
                        method: LinkMethod.Manual,
                        userId: userId!);
                }
            }

            var excludedCount = ContactList.Count - contactsToCreate.Count;
            var excludedMessage = excludedCount > 0 ? $" ({excludedCount} excluded)" : "";
            var outbreakMessage = OutbreakId.HasValue ? $" Linked to outbreak." : "";
            SuccessMessage = $"Successfully created {createdCount} new patients and linked {linkedCount} existing patients. Total {contactsToCreate.Count} contacts created{excludedMessage}.{outbreakMessage}";
            return RedirectToPage("/Cases/Details", new { id = CaseId });
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Error creating contacts: {ex.Message}";
            ShowReviewScreen = true;
            await LoadPageDataAsync();
            return Page();
        }
    }

    private async Task LoadContactClassifications()
    {
        ContactClassificationsList = new SelectList(
            await _context.ContactClassifications
                .Where(cc => cc.IsActive)
                .OrderBy(cc => cc.DisplayOrder)
                .ToListAsync(),
            "Id",
            "Name");
    }

    private async Task LoadOutbreaksAsync()
    {
        var activeOutbreaks = await _outbreakService.GetActiveOutbreaksAsync();
        OutbreaksList = new SelectList(activeOutbreaks, "Id", "Name", OutbreakId);
    }

    private void LoadExposureStatuses()
    {
        ExposureStatusList = new SelectList(new[]
        {
            new { Value = "ConfirmedExposure", Text = "Confirmed Exposure" },
            new { Value = "PotentialExposure", Text = "Potential Exposure" },
            new { Value = "UnderInvestigation", Text = "Under Investigation" },
            new { Value = "Unknown", Text = "Unknown" }
        }, "Value", "Text");
    }


    private DateTime? ParseDate(string? dateStr)
    {
        if (string.IsNullOrWhiteSpace(dateStr))
            return null;

        if (DateTime.TryParse(dateStr, out var date))
            return date;

        return null;
    }

    private string GetMatchReason(DuplicateDetectionResult result)
    {
        if (!result.IsPotentialDuplicate)
            return string.Empty;

        return result.Confidence switch
        {
            MatchConfidence.High => "High confidence match: Name, DOB, and contact info match",
            MatchConfidence.Medium => "Medium confidence: Name and DOB match",
            MatchConfidence.Low => "Low confidence: Partial match found",
            _ => "Potential duplicate detected"
        };
    }
}

public class BulkContactCsvRow
{
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? DateOfBirth { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? ParentGuardianName { get; set; }
    public string? ParentGuardianPhone { get; set; }
    public string? ExposureStatus { get; set; }
    public string? ConfidenceLevel { get; set; }
    public string? Notes { get; set; }
}

