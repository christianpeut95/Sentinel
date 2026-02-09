using Surveillance_MVP.Models;

namespace Surveillance_MVP.Services;

public interface IOutbreakService
{
    Task<Outbreak?> GetByIdAsync(int id);
    Task<List<Outbreak>> GetAllAsync(bool includeInactive = false);
    Task<List<Outbreak>> GetActiveOutbreaksAsync();
    Task<Outbreak> CreateAsync(Outbreak outbreak, string userId);
    Task<bool> UpdateAsync(Outbreak outbreak, string userId);
    Task<bool> DeleteAsync(int id, string userId);
    
    Task<bool> AddTeamMemberAsync(int outbreakId, string userId, OutbreakRole role, string addedBy);
    Task<bool> RemoveTeamMemberAsync(int outbreakId, string userId, string removedBy);
    Task<List<OutbreakTeamMember>> GetTeamMembersAsync(int outbreakId);
    
    Task<OutbreakCaseDefinition> CreateDefinitionAsync(OutbreakCaseDefinition definition, string userId);
    Task<List<OutbreakCaseDefinition>> GetDefinitionsAsync(int outbreakId, bool activeOnly = true);
    Task<OutbreakCaseDefinition?> GetActiveDefinitionAsync(int outbreakId, CaseClassification classification);
    
    Task<OutbreakCase> LinkCaseAsync(int outbreakId, Guid caseId, CaseClassification? classification, LinkMethod method, string userId, int? searchQueryId = null);
    Task<bool> UnlinkCaseAsync(int outbreakCaseId, string reason, string userId);
    Task<bool> ClassifyCaseAsync(int outbreakCaseId, CaseClassification classification, string? notes, string userId);
    Task<List<OutbreakCase>> GetOutbreakCasesAsync(int outbreakId, bool activeOnly = true);
    Task<List<OutbreakCase>> GetOutbreakContactsAsync(int outbreakId, bool activeOnly = true);
    Task<List<Case>> GetSuggestedCasesAsync(int outbreakId, int searchQueryId);
    
    Task<OutbreakSearchQuery> CreateSearchQueryAsync(OutbreakSearchQuery query, string userId);
    Task<List<OutbreakSearchQuery>> GetSearchQueriesAsync(int outbreakId);
    Task<List<Case>> ExecuteSearchQueryAsync(int queryId);
    Task<bool> ToggleAutoLinkAsync(int queryId, bool enable);
    
    Task<OutbreakTimeline> AddTimelineEventAsync(OutbreakTimeline timelineEvent, string userId);
    Task<List<OutbreakTimeline>> GetTimelineAsync(int outbreakId);
    
    Task<OutbreakStatistics> GetStatisticsAsync(int outbreakId);
    
    Task<bool> BulkAssignTaskAsync(int outbreakId, int taskTemplateId, List<Guid> caseIds, string userId);
    Task<bool> BulkAssignSurveyAsync(int outbreakId, int surveyTemplateId, List<Guid> caseIds, string userId);
}

public class OutbreakStatistics
{
    public int TotalCases { get; set; }
    public int ConfirmedCases { get; set; }
    public int ProbableCases { get; set; }
    public int SuspectCases { get; set; }
    public int TotalContacts { get; set; }
    public int TeamMemberCount { get; set; }
    public int DaysSinceStart { get; set; }
    
    // Demographics
    public double? MedianAge { get; set; }
    public int? MinAge { get; set; }
    public int? MaxAge { get; set; }
    public int MaleCount { get; set; }
    public int FemaleCount { get; set; }
    public int OtherSexCount { get; set; }
    public int UnknownSexCount { get; set; }
    
    // Epidemic Curve Data
    public List<EpiCurveDataPoint> EpiCurveData { get; set; } = new();
}

public class EpiCurveDataPoint
{
    public DateTime Date { get; set; }
    public int ConfirmedCount { get; set; }
    public int ProbableCount { get; set; }
    public int SuspectCount { get; set; }
    public int UnclassifiedCount { get; set; }
}

