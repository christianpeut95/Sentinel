using Microsoft.EntityFrameworkCore;
using Surveillance_MVP.Data;
using Surveillance_MVP.Models;

namespace Surveillance_MVP.Services;

public class OutbreakService : IOutbreakService
{
    private readonly ApplicationDbContext _context;
    private readonly ITaskService _taskService;

    public OutbreakService(ApplicationDbContext context, ITaskService taskService)
    {
        _context = context;
        _taskService = taskService;
    }

    public async Task<Outbreak?> GetByIdAsync(int id)
    {
        return await _context.Outbreaks
            .Include(o => o.PrimaryDisease)
            .Include(o => o.PrimaryLocation)
            .Include(o => o.PrimaryEvent)
            .Include(o => o.LeadInvestigator)
            .Include(o => o.TeamMembers.Where(tm => tm.IsActive))
                .ThenInclude(tm => tm.User)
            .FirstOrDefaultAsync(o => o.Id == id && !o.IsDeleted);
    }

    public async Task<List<Outbreak>> GetAllAsync(bool includeInactive = false)
    {
        var query = _context.Outbreaks
            .Include(o => o.PrimaryDisease)
            .Include(o => o.PrimaryLocation)
            .Include(o => o.PrimaryEvent)
            .Include(o => o.LeadInvestigator)
            .Where(o => !o.IsDeleted);

        if (!includeInactive)
        {
            query = query.Where(o => o.Status == OutbreakStatus.Active || o.Status == OutbreakStatus.Monitoring);
        }

        return await query.OrderByDescending(o => o.CreatedDate).ToListAsync();
    }

    public async Task<List<Outbreak>> GetActiveOutbreaksAsync()
    {
        return await _context.Outbreaks
            .Include(o => o.PrimaryDisease)
            .Include(o => o.PrimaryLocation)
            .Include(o => o.PrimaryEvent)
            .Where(o => !o.IsDeleted && o.Status == OutbreakStatus.Active)
            .OrderByDescending(o => o.StartDate)
            .ToListAsync();
    }

    public async Task<Outbreak> CreateAsync(Outbreak outbreak, string userId)
    {
        outbreak.CreatedDate = DateTime.UtcNow;
        outbreak.CreatedBy = userId;

        _context.Outbreaks.Add(outbreak);
        await _context.SaveChangesAsync();

        await AddTimelineEventAsync(new OutbreakTimeline
        {
            OutbreakId = outbreak.Id,
            EventDate = outbreak.StartDate,
            Title = "Outbreak Declared",
            Description = $"Outbreak '{outbreak.Name}' was created",
            EventType = TimelineEventType.OutbreakDeclared
        }, userId);

        return outbreak;
    }

    public async Task<bool> UpdateAsync(Outbreak outbreak, string userId)
    {
        outbreak.ModifiedDate = DateTime.UtcNow;
        outbreak.ModifiedBy = userId;

        _context.Outbreaks.Update(outbreak);
        return await _context.SaveChangesAsync() > 0;
    }

    public async Task<bool> DeleteAsync(int id, string userId)
    {
        var outbreak = await _context.Outbreaks.FindAsync(id);
        if (outbreak == null) return false;

        outbreak.IsDeleted = true;
        outbreak.ModifiedDate = DateTime.UtcNow;
        outbreak.ModifiedBy = userId;

        return await _context.SaveChangesAsync() > 0;
    }

    public async Task<bool> AddTeamMemberAsync(int outbreakId, string userId, OutbreakRole role, string addedBy)
    {
        var existing = await _context.OutbreakTeamMembers
            .FirstOrDefaultAsync(tm => tm.OutbreakId == outbreakId && tm.UserId == userId && tm.IsActive);

        if (existing != null) return false;

        var teamMember = new OutbreakTeamMember
        {
            OutbreakId = outbreakId,
            UserId = userId,
            Role = role,
            AssignedDate = DateTime.UtcNow,
            AssignedBy = addedBy,
            IsActive = true
        };

        _context.OutbreakTeamMembers.Add(teamMember);
        await _context.SaveChangesAsync();

        var user = await _context.Users.FindAsync(userId);
        await AddTimelineEventAsync(new OutbreakTimeline
        {
            OutbreakId = outbreakId,
            EventDate = DateTime.UtcNow,
            Title = "Team Member Added",
            Description = $"{user?.UserName ?? "User"} added as {role}",
            EventType = TimelineEventType.TeamMemberAdded
        }, addedBy);

        return true;
    }

    public async Task<bool> RemoveTeamMemberAsync(int outbreakId, string userId, string removedBy)
    {
        var teamMember = await _context.OutbreakTeamMembers
            .FirstOrDefaultAsync(tm => tm.OutbreakId == outbreakId && tm.UserId == userId && tm.IsActive);

        if (teamMember == null) return false;

        teamMember.IsActive = false;
        teamMember.RemovedDate = DateTime.UtcNow;
        teamMember.RemovedBy = removedBy;

        await _context.SaveChangesAsync();

        var user = await _context.Users.FindAsync(userId);
        await AddTimelineEventAsync(new OutbreakTimeline
        {
            OutbreakId = outbreakId,
            EventDate = DateTime.UtcNow,
            Title = "Team Member Removed",
            Description = $"{user?.UserName ?? "User"} removed from team",
            EventType = TimelineEventType.TeamMemberRemoved
        }, removedBy);

        return true;
    }

    public async Task<List<OutbreakTeamMember>> GetTeamMembersAsync(int outbreakId)
    {
        return await _context.OutbreakTeamMembers
            .Include(tm => tm.User)
            .Where(tm => tm.OutbreakId == outbreakId && tm.IsActive)
            .OrderBy(tm => tm.Role)
            .ToListAsync();
    }

    public async Task<OutbreakCaseDefinition> CreateDefinitionAsync(OutbreakCaseDefinition definition, string userId)
    {
        var maxVersion = await _context.OutbreakCaseDefinitions
            .Where(d => d.OutbreakId == definition.OutbreakId && d.Classification == definition.Classification)
            .MaxAsync(d => (int?)d.Version) ?? 0;

        definition.Version = maxVersion + 1;
        definition.CreatedDate = DateTime.UtcNow;
        definition.CreatedBy = userId;

        _context.OutbreakCaseDefinitions.Add(definition);
        await _context.SaveChangesAsync();

        await AddTimelineEventAsync(new OutbreakTimeline
        {
            OutbreakId = definition.OutbreakId,
            EventDate = DateTime.UtcNow,
            Title = "Case Definition Updated",
            Description = $"{definition.Classification} case definition updated to version {definition.Version}",
            EventType = TimelineEventType.DefinitionUpdated
        }, userId);

        return definition;
    }

    public async Task<List<OutbreakCaseDefinition>> GetDefinitionsAsync(int outbreakId, bool activeOnly = true)
    {
        var query = _context.OutbreakCaseDefinitions
            .Where(d => d.OutbreakId == outbreakId);

        if (activeOnly)
        {
            query = query.Where(d => d.IsActive);
        }

        return await query
            .OrderBy(d => d.Classification)
            .ThenByDescending(d => d.Version)
            .ToListAsync();
    }

    public async Task<OutbreakCaseDefinition?> GetActiveDefinitionAsync(int outbreakId, CaseClassification classification)
    {
        return await _context.OutbreakCaseDefinitions
            .Where(d => d.OutbreakId == outbreakId && d.Classification == classification && d.IsActive)
            .OrderByDescending(d => d.Version)
            .FirstOrDefaultAsync();
    }

    public async Task<OutbreakCase> LinkCaseAsync(int outbreakId, Guid caseId, CaseClassification? classification, LinkMethod method, string userId, int? searchQueryId = null)
    {
        var existingLink = await _context.OutbreakCases
            .FirstOrDefaultAsync(oc => oc.OutbreakId == outbreakId && oc.CaseId == caseId && oc.IsActive);

        if (existingLink != null) return existingLink;

        var outbreakCase = new OutbreakCase
        {
            OutbreakId = outbreakId,
            CaseId = caseId,
            Classification = classification,
            LinkMethod = method,
            SearchQueryId = searchQueryId,
            LinkedDate = DateTime.UtcNow,
            LinkedBy = userId,
            IsActive = true
        };

        _context.OutbreakCases.Add(outbreakCase);
        await _context.SaveChangesAsync();

        var caseEntity = await _context.Cases
            .Include(c => c.Patient)
            .FirstOrDefaultAsync(c => c.Id == caseId);

        var eventType = caseEntity?.Type == CaseType.Contact 
            ? TimelineEventType.ContactAdded 
            : TimelineEventType.CaseAdded;

        var title = caseEntity?.Type == CaseType.Contact 
            ? "Contact Added" 
            : "Case Added";

        await AddTimelineEventAsync(new OutbreakTimeline
        {
            OutbreakId = outbreakId,
            EventDate = DateTime.UtcNow,
            Title = title,
            Description = $"{caseEntity?.Patient?.GivenName} {caseEntity?.Patient?.FamilyName} linked to outbreak",
            EventType = eventType,
            RelatedCaseId = caseId
        }, userId);

        return outbreakCase;
    }

    public async Task<bool> UnlinkCaseAsync(int outbreakCaseId, string reason, string userId)
    {
        var outbreakCase = await _context.OutbreakCases
            .Include(oc => oc.Case)
                .ThenInclude(c => c.Patient)
            .FirstOrDefaultAsync(oc => oc.Id == outbreakCaseId);

        if (outbreakCase == null) return false;

        outbreakCase.IsActive = false;
        outbreakCase.UnlinkedDate = DateTime.UtcNow;
        outbreakCase.UnlinkedBy = userId;
        outbreakCase.UnlinkReason = reason;

        await _context.SaveChangesAsync();

        var eventType = outbreakCase.Case?.Type == CaseType.Contact 
            ? TimelineEventType.ContactRemoved 
            : TimelineEventType.CaseRemoved;

        var title = outbreakCase.Case?.Type == CaseType.Contact 
            ? "Contact Removed" 
            : "Case Removed";

        await AddTimelineEventAsync(new OutbreakTimeline
        {
            OutbreakId = outbreakCase.OutbreakId,
            EventDate = DateTime.UtcNow,
            Title = title,
            Description = $"{outbreakCase.Case?.Patient?.GivenName} {outbreakCase.Case?.Patient?.FamilyName} unlinked: {reason}",
            EventType = eventType,
            RelatedCaseId = outbreakCase.CaseId
        }, userId);

        return true;
    }

    public async Task<bool> ClassifyCaseAsync(int outbreakCaseId, CaseClassification classification, string? notes, string userId)
    {
        var outbreakCase = await _context.OutbreakCases
            .Include(oc => oc.Case)
                .ThenInclude(c => c.Patient)
            .FirstOrDefaultAsync(oc => oc.Id == outbreakCaseId);

        if (outbreakCase == null) return false;

        var previousClassification = outbreakCase.Classification;
        outbreakCase.Classification = classification;
        outbreakCase.ClassificationDate = DateTime.UtcNow;
        outbreakCase.ClassifiedBy = userId;
        outbreakCase.ClassificationNotes = notes;

        await _context.SaveChangesAsync();

        var title = previousClassification.HasValue ? "Case Reclassified" : "Case Classified";
        var description = previousClassification.HasValue 
            ? $"{outbreakCase.Case?.Patient?.GivenName} {outbreakCase.Case?.Patient?.FamilyName} reclassified from {previousClassification.Value} to {classification}"
            : $"{outbreakCase.Case?.Patient?.GivenName} {outbreakCase.Case?.Patient?.FamilyName} classified as {classification}";

        await AddTimelineEventAsync(new OutbreakTimeline
        {
            OutbreakId = outbreakCase.OutbreakId,
            EventDate = DateTime.UtcNow,
            Title = title,
            Description = description,
            EventType = TimelineEventType.CaseClassified,
            RelatedCaseId = outbreakCase.CaseId
        }, userId);

        return true;
    }

    public async Task<List<OutbreakCase>> GetOutbreakCasesAsync(int outbreakId, bool activeOnly = true)
    {
        var query = _context.OutbreakCases
            .Include(oc => oc.Case)
                .ThenInclude(c => c.Patient)
            .Include(oc => oc.Case.Disease)
            .Where(oc => oc.OutbreakId == outbreakId && oc.Case.Type == CaseType.Case);

        if (activeOnly)
        {
            query = query.Where(oc => oc.IsActive);
        }

        return await query.OrderByDescending(oc => oc.LinkedDate).ToListAsync();
    }

    public async Task<List<OutbreakCase>> GetOutbreakContactsAsync(int outbreakId, bool activeOnly = true)
    {
        var query = _context.OutbreakCases
            .Include(oc => oc.Case)
                .ThenInclude(c => c.Patient)
            .Include(oc => oc.Case.Disease)
            .Where(oc => oc.OutbreakId == outbreakId && oc.Case.Type == CaseType.Contact);

        if (activeOnly)
        {
            query = query.Where(oc => oc.IsActive);
        }

        return await query.OrderByDescending(oc => oc.LinkedDate).ToListAsync();
    }

    public async Task<List<Case>> GetSuggestedCasesAsync(int outbreakId, int searchQueryId)
    {
        var query = await _context.OutbreakSearchQueries.FindAsync(searchQueryId);
        if (query == null) return new List<Case>();

        return await ExecuteSearchQueryAsync(searchQueryId);
    }

    public async Task<OutbreakSearchQuery> CreateSearchQueryAsync(OutbreakSearchQuery query, string userId)
    {
        query.CreatedDate = DateTime.UtcNow;
        query.CreatedBy = userId;

        _context.OutbreakSearchQueries.Add(query);
        await _context.SaveChangesAsync();

        return query;
    }

    public async Task<List<OutbreakSearchQuery>> GetSearchQueriesAsync(int outbreakId)
    {
        return await _context.OutbreakSearchQueries
            .Where(q => q.OutbreakId == outbreakId && q.IsActive)
            .OrderByDescending(q => q.CreatedDate)
            .ToListAsync();
    }

    public async Task<List<Case>> ExecuteSearchQueryAsync(int queryId)
    {
        var query = await _context.OutbreakSearchQueries.FindAsync(queryId);
        if (query == null) return new List<Case>();

        return new List<Case>();
    }

    public async Task<bool> ToggleAutoLinkAsync(int queryId, bool enable)
    {
        var query = await _context.OutbreakSearchQueries.FindAsync(queryId);
        if (query == null) return false;

        query.IsAutoLink = enable;
        return await _context.SaveChangesAsync() > 0;
    }

    public async Task<OutbreakTimeline> AddTimelineEventAsync(OutbreakTimeline timelineEvent, string userId)
    {
        timelineEvent.CreatedDate = DateTime.UtcNow;
        timelineEvent.CreatedBy = userId;

        _context.OutbreakTimelines.Add(timelineEvent);
        await _context.SaveChangesAsync();

        return timelineEvent;
    }

    public async Task<List<OutbreakTimeline>> GetTimelineAsync(int outbreakId)
    {
        return await _context.OutbreakTimelines
            .Where(t => t.OutbreakId == outbreakId)
            .OrderByDescending(t => t.EventDate)
            .ToListAsync();
    }

    public async Task<OutbreakStatistics> GetStatisticsAsync(int outbreakId)
    {
        var outbreak = await _context.Outbreaks.FindAsync(outbreakId);
        var cases = await GetOutbreakCasesAsync(outbreakId);
        var contacts = await GetOutbreakContactsAsync(outbreakId);
        var teamMembers = await GetTeamMembersAsync(outbreakId);

        // Calculate demographics
        var patientsWithAge = cases
            .Where(c => c.Case?.Patient?.DateOfBirth != null)
            .Select(c => new
            {
                Age = DateTime.UtcNow.Year - c.Case!.Patient!.DateOfBirth!.Value.Year,
                Sex = c.Case.Patient.SexAtBirthId
            })
            .ToList();

        var ages = patientsWithAge.Select(p => p.Age).OrderBy(a => a).ToList();
        double? medianAge = null;
        if (ages.Any())
        {
            int count = ages.Count;
            medianAge = count % 2 == 0
                ? (ages[count / 2 - 1] + ages[count / 2]) / 2.0
                : ages[count / 2];
        }

        // Calculate epidemic curve data
        var casesByDate = cases
            .GroupBy(c => (c.Case?.DateOfOnset ?? c.Case?.DateOfNotification ?? c.LinkedDate).Date)
            .ToDictionary(
                g => g.Key,
                g => new
                {
                    ConfirmedCount = g.Count(c => c.Classification == CaseClassification.Confirmed),
                    ProbableCount = g.Count(c => c.Classification == CaseClassification.Probable),
                    SuspectCount = g.Count(c => c.Classification == CaseClassification.Suspect),
                    UnclassifiedCount = g.Count(c => !c.Classification.HasValue)
                }
            );

        // Fill in missing dates with zeros (no gaps in epidemic curve)
        var epiCurveData = new List<EpiCurveDataPoint>();
        if (casesByDate.Any())
        {
            var minDate = casesByDate.Keys.Min().AddDays(-3); // Add 3 days padding before
            var maxDate = casesByDate.Keys.Max().AddDays(3); // Add 3 days padding after
            
            for (var date = minDate; date <= maxDate; date = date.AddDays(1))
            {
                if (casesByDate.ContainsKey(date))
                {
                    var data = casesByDate[date];
                    epiCurveData.Add(new EpiCurveDataPoint
                    {
                        Date = date,
                        ConfirmedCount = data.ConfirmedCount,
                        ProbableCount = data.ProbableCount,
                        SuspectCount = data.SuspectCount,
                        UnclassifiedCount = data.UnclassifiedCount
                    });
                }
                else
                {
                    // Fill gap with zeros
                    epiCurveData.Add(new EpiCurveDataPoint
                    {
                        Date = date,
                        ConfirmedCount = 0,
                        ProbableCount = 0,
                        SuspectCount = 0,
                        UnclassifiedCount = 0
                    });
                }
            }
        }

        return new OutbreakStatistics
        {
            TotalCases = cases.Count,
            ConfirmedCases = cases.Count(c => c.Classification == CaseClassification.Confirmed),
            ProbableCases = cases.Count(c => c.Classification == CaseClassification.Probable),
            SuspectCases = cases.Count(c => c.Classification == CaseClassification.Suspect),
            TotalContacts = contacts.Count,
            TeamMemberCount = teamMembers.Count,
            DaysSinceStart = outbreak != null ? (DateTime.UtcNow - outbreak.StartDate).Days : 0,
            
            // Demographics
            MedianAge = medianAge,
            MinAge = ages.Any() ? ages.Min() : (int?)null,
            MaxAge = ages.Any() ? ages.Max() : (int?)null,
            MaleCount = cases.Count(c => c.Case?.Patient?.SexAtBirthId == 1), // Assuming 1 = Male
            FemaleCount = cases.Count(c => c.Case?.Patient?.SexAtBirthId == 2), // Assuming 2 = Female
            OtherSexCount = cases.Count(c => c.Case?.Patient?.SexAtBirthId != null && c.Case.Patient.SexAtBirthId > 2),
            UnknownSexCount = cases.Count(c => c.Case?.Patient?.SexAtBirthId == null),
            
            // Epidemic Curve
            EpiCurveData = epiCurveData
        };
    }


    public async Task<bool> BulkAssignTaskAsync(int outbreakId, int taskTemplateId, List<Guid> caseIds, string userId)
    {
        // TODO: Implement bulk task assignment
        await AddTimelineEventAsync(new OutbreakTimeline
        {
            OutbreakId = outbreakId,
            EventDate = DateTime.UtcNow,
            Title = "Bulk Task Assigned",
            Description = $"Task assigned to {caseIds.Count} cases/contacts",
            EventType = TimelineEventType.BulkTaskAssigned
        }, userId);

        return true;
    }

    public async Task<bool> BulkAssignSurveyAsync(int outbreakId, int surveyTemplateId, List<Guid> caseIds, string userId)
    {
        // TODO: Implement bulk survey assignment
        await AddTimelineEventAsync(new OutbreakTimeline
        {
            OutbreakId = outbreakId,
            EventDate = DateTime.UtcNow,
            Title = "Bulk Survey Assigned",
            Description = $"Survey assigned to {caseIds.Count} cases/contacts",
            EventType = TimelineEventType.BulkSurveyAssigned
        }, userId);

        return true;
    }
}
