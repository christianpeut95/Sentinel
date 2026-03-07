namespace Sentinel.Models;

public enum OutbreakType
{
    Traditional = 1,
    LocationBased = 2,
    EventBased = 3
}

public enum OutbreakStatus
{
    Active = 1,
    Monitoring = 2,
    Resolved = 3,
    Closed = 4
}

public enum OutbreakRole
{
    LeadInvestigator = 1,
    Investigator = 2,
    DataManager = 3,
    LabLiaison = 4,
    CommunicationsOfficer = 5,
    TeamMember = 6
}

public enum CaseClassification
{
    Confirmed = 1,
    Probable = 2,
    Suspect = 3,
    NotACase = 4
}

public enum LinkMethod
{
    Manual = 1,
    AutoSuggested = 2,
    SearchQuery = 3
}

public enum TimelineEventType
{
    OutbreakDeclared = 1,
    CaseAdded = 2,
    CaseRemoved = 3,
    ContactAdded = 4,
    ContactRemoved = 5,
    CaseClassified = 6,
    DefinitionUpdated = 7,
    TeamMemberAdded = 8,
    TeamMemberRemoved = 9,
    FileUploaded = 10,
    MeetingScheduled = 11,
    PublicHealthAction = 12,
    LabResultReceived = 13,
    BulkTaskAssigned = 14,
    BulkSurveyAssigned = 14,
    StatusChanged = 15,
    Other = 99
}
