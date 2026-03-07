using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Sentinel.Data;
using Sentinel.Models;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;

namespace Sentinel.Pages.Outbreaks;

[Authorize(Policy = "Permission.Outbreak.Create")]
public class CreateFromExposureModel : PageModel
{
    private readonly ApplicationDbContext _context;

    public CreateFromExposureModel(ApplicationDbContext context)
    {
        _context = context;
    }

    // Route parameters
    [BindProperty(SupportsGet = true)]
    public Guid CaseId { get; set; }

    [BindProperty(SupportsGet = true)]
    public Guid? LocationId { get; set; }

    [BindProperty(SupportsGet = true)]
    public Guid? EventId { get; set; }

    [BindProperty(SupportsGet = true)]
    public Guid? ExposureId { get; set; }

    // Page data
    public Case SourceCase { get; set; } = default!;
    public Location? Location { get; set; }
    public Event? Event { get; set; }
    public int ExistingContactCount { get; set; }

    // Form inputs
    [BindProperty]
    public InputModel Input { get; set; } = new();

    [TempData]
    public string? SuccessMessage { get; set; }

    [TempData]
    public string? ErrorMessage { get; set; }

    public class InputModel
    {
        [Required]
        [StringLength(200)]
        public string Name { get; set; } = string.Empty;

        [StringLength(1000)]
        public string? Description { get; set; }

        public DateTime StartDate { get; set; } = DateTime.Today;
    }

    public async Task<IActionResult> OnGetAsync()
    {
        // Load source case
        SourceCase = await _context.Cases
            .Include(c => c.Patient)
            .Include(c => c.Disease)
            .FirstOrDefaultAsync(c => c.Id == CaseId);

        if (SourceCase == null)
            return NotFound();

        // Load location if specified
        if (LocationId.HasValue)
        {
            Location = await _context.Locations
                .Include(l => l.LocationType)
                .FirstOrDefaultAsync(l => l.Id == LocationId.Value);

            // Auto-generate name
            Input.Name = $"{SourceCase.Disease?.Name ?? "Investigation"} - {Location?.Name ?? "Unknown Location"} Contact Tracing";
        }
        // Load event if specified
        else if (EventId.HasValue)
        {
            Event = await _context.Events
                .Include(e => e.Location)
                .Include(e => e.EventType)
                .FirstOrDefaultAsync(e => e.Id == EventId.Value);

            // Auto-generate name
            Input.Name = $"{SourceCase.Disease?.Name ?? "Investigation"} - {Event?.Name ?? "Unknown Event"} Contact Tracing";
        }
        // No location/event - general contact tracing
        else
        {
            Input.Name = $"{SourceCase.Disease?.Name ?? "Investigation"} - {SourceCase.FriendlyId} Contact Tracing";
        }

        // Count existing contacts linked to this exposure
        if (ExposureId.HasValue)
        {
            // Check if this is a transmission (current case is source) or acquisition (current case is exposed)
            var exposure = await _context.ExposureEvents
                .FirstOrDefaultAsync(e => e.Id == ExposureId.Value);

            if (exposure != null)
            {
                // If current case is the SOURCE, count transmissions (exposed cases)
                if (exposure.SourceCaseId == CaseId)
                {
                    ExistingContactCount = await _context.ExposureEvents
                        .Where(e => e.SourceCaseId == CaseId && 
                                   (e.LocationId == LocationId || e.EventId == EventId || (!LocationId.HasValue && !EventId.HasValue)) &&
                                   e.ExposureType == ExposureType.Contact &&
                                   !e.IsDeleted)
                        .CountAsync();
                }
                // If current case is EXPOSED, count acquisitions (source cases)
                else
                {
                    ExistingContactCount = await _context.ExposureEvents
                        .Where(e => e.ExposedCaseId == CaseId && 
                                   (e.LocationId == LocationId || e.EventId == EventId || (!LocationId.HasValue && !EventId.HasValue)) &&
                                   e.ExposureType == ExposureType.Contact &&
                                   e.SourceCaseId.HasValue &&
                                   !e.IsDeleted)
                        .CountAsync();
                }
            }
        }
        else
        {
            // No specific exposure - count ALL transmissions for this case
            ExistingContactCount = await _context.ExposureEvents
                .Where(e => e.SourceCaseId == CaseId &&
                           e.ExposureType == ExposureType.Contact &&
                           !e.IsDeleted)
                .CountAsync();
        }

        Input.StartDate = SourceCase.DateOfNotification ?? DateTime.Today;
        Input.Description = LocationId.HasValue || EventId.HasValue
            ? $"Contact tracing event for {SourceCase.Disease?.Name} case {SourceCase.FriendlyId} at specific location/event"
            : $"General contact tracing event for {SourceCase.Disease?.Name} case {SourceCase.FriendlyId}";

        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            await OnGetAsync();
            return Page();
        }

        // Load source case (required for DiseaseId)
        SourceCase = await _context.Cases
            .Include(c => c.Patient)
            .Include(c => c.Disease)
            .FirstOrDefaultAsync(c => c.Id == CaseId);

        if (SourceCase == null)
            return NotFound();

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        try
        {
            // Determine outbreak type
            var outbreakType = EventId.HasValue ? OutbreakType.EventBased : OutbreakType.LocationBased;

            // Get "Under Investigation" status ID
            var underInvestigationStatus = await _context.CaseStatuses
                .FirstOrDefaultAsync(cs => cs.Name == "Under Investigation");

            // Create outbreak
            var outbreak = new Outbreak
            {
                Id = default, // EF will auto-generate
                Name = Input.Name,
                Description = Input.Description,
                Type = outbreakType,
                Status = OutbreakStatus.Active,
                ConfirmationStatusId = underInvestigationStatus?.Id, // Default to "Under Investigation"
                StartDate = Input.StartDate,
                IndexCaseId = CaseId,
                PrimaryLocationId = LocationId,
                PrimaryEventId = EventId,
                PrimaryDiseaseId = SourceCase.DiseaseId,
                LeadInvestigatorId = userId,
                CreatedDate = DateTime.UtcNow,
                CreatedBy = userId
            };

            _context.Outbreaks.Add(outbreak);
            await _context.SaveChangesAsync();


            // Link the index case to the outbreak
            var indexOutbreakCase = new OutbreakCase
            {
                OutbreakId = outbreak.Id,
                CaseId = CaseId,
                IsIndexCase = true,
                LinkedDate = DateTime.UtcNow,
                LinkedBy = userId
            };

            _context.OutbreakCases.Add(indexOutbreakCase);

            // Link all existing contacts at this location/event to the outbreak
            // If no location/event specified, link ALL contacts
            if (LocationId.HasValue || EventId.HasValue)
            {
                // Specific location/event - only link contacts from that exposure
                
                // Get contacts where current case is the SOURCE (Transmissions - people this case exposed)
                var transmissionContacts = await _context.ExposureEvents
                    .Where(e => e.SourceCaseId == CaseId &&
                               (e.LocationId == LocationId || e.EventId == EventId) &&
                               e.ExposureType == ExposureType.Contact &&
                               !e.IsDeleted)
                    .Select(e => e.ExposedCaseId)
                    .Distinct()
                    .ToListAsync();

                // Get contacts where current case was EXPOSED (Acquisitions - sources that exposed this case)
                var acquisitionContacts = await _context.ExposureEvents
                    .Where(e => e.ExposedCaseId == CaseId &&
                               (e.LocationId == LocationId || e.EventId == EventId) &&
                               e.ExposureType == ExposureType.Contact &&
                               e.SourceCaseId.HasValue &&
                               !e.IsDeleted)
                    .Select(e => e.SourceCaseId!.Value)
                    .Distinct()
                    .ToListAsync();

                // Combine both lists (remove duplicates)
                var allRelatedContacts = transmissionContacts.Concat(acquisitionContacts).Distinct().ToList();

                foreach (var contactId in allRelatedContacts)
                {
                    var outbreakCase = new OutbreakCase
                    {
                        OutbreakId = outbreak.Id,
                        CaseId = contactId,
                        IsIndexCase = false,
                        LinkedDate = DateTime.UtcNow,
                        LinkedBy = userId,
                        LinkMethod = LinkMethod.AutoSuggested
                    };

                    _context.OutbreakCases.Add(outbreakCase);
                }

                ExistingContactCount = allRelatedContacts.Count;
            }
            else
            {
                // No specific location/event - link ALL contacts for this case
                
                // Get ALL transmission contacts (people this case exposed)
                var allTransmissionContacts = await _context.ExposureEvents
                    .Where(e => e.SourceCaseId == CaseId &&
                               e.ExposureType == ExposureType.Contact &&
                               !e.IsDeleted)
                    .Select(e => e.ExposedCaseId)
                    .Distinct()
                    .ToListAsync();

                // Get ALL acquisition contacts (sources that exposed this case)
                var allAcquisitionContacts = await _context.ExposureEvents
                    .Where(e => e.ExposedCaseId == CaseId &&
                               e.ExposureType == ExposureType.Contact &&
                               e.SourceCaseId.HasValue &&
                               !e.IsDeleted)
                    .Select(e => e.SourceCaseId!.Value)
                    .Distinct()
                    .ToListAsync();

                // Combine both lists (remove duplicates)
                var allContacts = allTransmissionContacts.Concat(allAcquisitionContacts).Distinct().ToList();

                foreach (var contactId in allContacts)
                {
                    var outbreakCase = new OutbreakCase
                    {
                        OutbreakId = outbreak.Id,
                        CaseId = contactId,
                        IsIndexCase = false,
                        LinkedDate = DateTime.UtcNow,
                        LinkedBy = userId,
                        LinkMethod = LinkMethod.AutoSuggested
                    };

                    _context.OutbreakCases.Add(outbreakCase);
                }

                ExistingContactCount = allContacts.Count;
            }

            await _context.SaveChangesAsync();

            SuccessMessage = $"Contact Tracing Event '{Input.Name}' created successfully with {ExistingContactCount} linked contacts.";
            return RedirectToPage("/Outbreaks/Details", new { id = outbreak.Id });
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Error creating contact tracing event: {ex.Message}";
            await OnGetAsync();
            return Page();
        }
    }
}
