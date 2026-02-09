using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Surveillance_MVP.Data;
using Surveillance_MVP.Services;
using Surveillance_MVP.Models;
using System.Security.Claims;

namespace Surveillance_MVP.Pages.Outbreaks;

[Authorize(Policy = "Permission.Outbreak.Edit")]
public class CaseDefinitionsModel : PageModel
{
    private readonly ApplicationDbContext _context;
    private readonly IOutbreakService _outbreakService;

    public CaseDefinitionsModel(ApplicationDbContext context, IOutbreakService outbreakService)
    {
        _context = context;
        _outbreakService = outbreakService;
    }

    public Outbreak Outbreak { get; set; } = null!;
    public List<OutbreakCaseDefinition> Definitions { get; set; } = new();

    // Group definitions by classification
    public OutbreakCaseDefinition? ConfirmedDefinition { get; set; }
    public OutbreakCaseDefinition? ProbableDefinition { get; set; }
    public OutbreakCaseDefinition? SuspectDefinition { get; set; }
    public OutbreakCaseDefinition? NotACaseDefinition { get; set; }

    [BindProperty]
    public OutbreakCaseDefinition Definition { get; set; } = new();
    
    [BindProperty]
    public int EditingDefinitionId { get; set; }

    [TempData]
    public string? SuccessMessage { get; set; }

    [TempData]
    public string? ErrorMessage { get; set; }

    public async Task<IActionResult> OnGetAsync(int id)
    {
        var outbreak = await _outbreakService.GetByIdAsync(id);
        if (outbreak == null)
        {
            return NotFound();
        }

        Outbreak = outbreak;
        await LoadDefinitionsAsync();

        return Page();
    }

    public async Task<IActionResult> OnPostSaveDefinitionAsync(int id)
    {
        var outbreak = await _outbreakService.GetByIdAsync(id);
        if (outbreak == null)
        {
            return NotFound();
        }

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
        
        // Check if updating existing or creating new
        if (EditingDefinitionId > 0)
        {
            var existing = await _context.OutbreakCaseDefinitions.FindAsync(EditingDefinitionId);
            if (existing != null)
            {
                existing.DefinitionName = Definition.DefinitionName;
                existing.DefinitionText = Definition.DefinitionText;
                existing.CriteriaJson = string.IsNullOrWhiteSpace(Definition.CriteriaJson) ? "{}" : Definition.CriteriaJson;
                existing.Notes = Definition.Notes;
                
                await _context.SaveChangesAsync();
                
                await _outbreakService.AddTimelineEventAsync(new OutbreakTimeline
                {
                    OutbreakId = id,
                    EventDate = DateTime.UtcNow,
                    Title = "Case Definition Updated",
                    Description = $"{existing.Classification} definition updated: {existing.DefinitionName}",
                    EventType = TimelineEventType.DefinitionUpdated
                }, userId);
                
                SuccessMessage = "Case definition updated successfully.";
            }
        }
        else
        {
            // Create new definition
            var newDefinition = new OutbreakCaseDefinition
            {
                OutbreakId = id,
                Classification = Definition.Classification,
                DefinitionName = Definition.DefinitionName,
                DefinitionText = Definition.DefinitionText,
                CriteriaJson = string.IsNullOrWhiteSpace(Definition.CriteriaJson) ? "{}" : Definition.CriteriaJson,
                Notes = Definition.Notes,
                Version = await GetNextVersionAsync(id, Definition.Classification),
                EffectiveDate = DateTime.UtcNow,
                CreatedDate = DateTime.UtcNow,
                CreatedBy = userId,
                IsActive = true
            };

            // Deactivate previous definition of same classification
            var previousDefinitions = await _context.OutbreakCaseDefinitions
                .Where(d => d.OutbreakId == id && 
                           d.Classification == Definition.Classification && 
                           d.IsActive)
                .ToListAsync();

            foreach (var prev in previousDefinitions)
            {
                prev.IsActive = false;
                prev.ExpiryDate = DateTime.UtcNow;
            }

            _context.OutbreakCaseDefinitions.Add(newDefinition);
            await _context.SaveChangesAsync();

            // Log to timeline
            await _outbreakService.AddTimelineEventAsync(new OutbreakTimeline
            {
                OutbreakId = id,
                EventDate = DateTime.UtcNow,
                Title = "Case Definition Created",
                Description = $"{newDefinition.Classification} case definition created: {newDefinition.DefinitionName}",
                EventType = TimelineEventType.DefinitionUpdated
            }, userId);

            SuccessMessage = $"{Definition.Classification} case definition created successfully.";
        }

        return RedirectToPage(new { id });
    }

    public async Task<IActionResult> OnPostActivateDefinitionAsync(int id, int definitionId)
    {
        var definition = await _context.OutbreakCaseDefinitions.FindAsync(definitionId);
        if (definition == null || definition.OutbreakId != id)
        {
            ErrorMessage = "Definition not found.";
            return RedirectToPage(new { id });
        }

        // Deactivate all other definitions of same classification
        var others = await _context.OutbreakCaseDefinitions
            .Where(d => d.OutbreakId == id && 
                       d.Classification == definition.Classification && 
                       d.Id != definitionId)
            .ToListAsync();

        foreach (var other in others)
        {
            other.IsActive = false;
            other.ExpiryDate = DateTime.UtcNow;
        }

        definition.IsActive = true;
        definition.EffectiveDate = DateTime.UtcNow;
        definition.ExpiryDate = null;

        await _context.SaveChangesAsync();

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
        await _outbreakService.AddTimelineEventAsync(new OutbreakTimeline
        {
            OutbreakId = id,
            EventDate = DateTime.UtcNow,
            Title = "Case Definition Activated",
            Description = $"{definition.Classification} definition activated: {definition.DefinitionName}",
            EventType = TimelineEventType.DefinitionUpdated
        }, userId);

        SuccessMessage = "Case definition activated successfully.";
        return RedirectToPage(new { id });
    }

    private async Task LoadDefinitionsAsync()
    {
        Definitions = await _context.OutbreakCaseDefinitions
            .Where(d => d.OutbreakId == Outbreak.Id)
            .OrderByDescending(d => d.Version)
            .ToListAsync();

        // Get active definition for each classification
        ConfirmedDefinition = Definitions
            .Where(d => d.Classification == CaseClassification.Confirmed && d.IsActive)
            .FirstOrDefault();

        ProbableDefinition = Definitions
            .Where(d => d.Classification == CaseClassification.Probable && d.IsActive)
            .FirstOrDefault();

        SuspectDefinition = Definitions
            .Where(d => d.Classification == CaseClassification.Suspect && d.IsActive)
            .FirstOrDefault();

        NotACaseDefinition = Definitions
            .Where(d => d.Classification == CaseClassification.NotACase && d.IsActive)
            .FirstOrDefault();
    }

    private async Task<int> GetNextVersionAsync(int outbreakId, CaseClassification classification)
    {
        var existingVersions = await _context.OutbreakCaseDefinitions
            .Where(d => d.OutbreakId == outbreakId && d.Classification == classification)
            .Select(d => d.Version)
            .ToListAsync();

        var maxVersion = existingVersions.Any() ? existingVersions.Max() : 0;
        return maxVersion + 1;
    }
}
