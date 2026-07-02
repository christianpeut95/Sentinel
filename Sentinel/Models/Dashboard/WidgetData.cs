namespace Sentinel.Models.Dashboard
{
    public class WidgetData
    {
        public string WidgetId { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public object? Data { get; set; }
        public DateTime? LastUpdated { get; set; }
        public string? ErrorMessage { get; set; }
    }

    // Specific widget data models
    public class RecentActivityData
    {
        public List<ActivityItem> Items { get; set; } = new();
        public int TotalCount { get; set; }
    }

    public class ActivityItem
    {
        public string ActivityType { get; set; } = string.Empty;
        public string EntityType { get; set; } = string.Empty;
        public string EntityId { get; set; } = string.Empty;
        public string DisplayText { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty; // "outbreak", "watch", "resolved", "info"
        public DateTime OccurredAt { get; set; }
        public string? DiseaseCode { get; set; }
        public string? Region { get; set; }
    }

    public class CasesByDiseaseData
    {
        public List<DiseaseCount> Diseases { get; set; } = new();
    }

    public class DiseaseCount
    {
        public Guid DiseaseId { get; set; }
        public string DiseaseName { get; set; } = string.Empty;
        public int ConfirmedCount { get; set; }
        public int ProbableCount { get; set; }
        public int TotalCount { get; set; }
        public int Delta7Day { get; set; }
        public string StatusColor { get; set; } = "clear"; // "outbreak", "watch", "clear"
    }

    public class HL7OverviewData
    {
        public int ProcessedCount { get; set; }
        public int RejectedCount { get; set; }
        public int AwaitingReviewCount { get; set; }
        public double AvgProcessingTimeMs { get; set; }
        public double RejectionRate { get; set; }
        public string HealthStatus { get; set; } = "healthy"; // "healthy", "warning", "error"
    }

    public class TasksAndSurveysData
    {
        public int OutstandingTasks { get; set; }
        public int CompletedToday { get; set; }
        public int OverdueTasks { get; set; }
        public List<TaskSummary> TopTaskTypes { get; set; } = new();
    }

    public class TaskSummary
    {
        public string TaskType { get; set; } = string.Empty;
        public int Count { get; set; }
        public int OverdueCount { get; set; }
    }

    public class OutbreakTrackerData
    {
        public List<OutbreakSummary> ActiveOutbreaks { get; set; } = new();
    }

    public class OutbreakSummary
    {
        public Guid OutbreakId { get; set; }
        public string OutbreakName { get; set; } = string.Empty;
        public int LinkedCasesCount { get; set; }
        public int DaysSinceDeclaration { get; set; }
        public int TeamMemberCount { get; set; }
        public string LatestActivity { get; set; } = string.Empty;
        public DateTime? LatestActivityTime { get; set; }
        public bool IsNew { get; set; }
    }

    public class DataReviewQueueData
    {
        public int TotalItems { get; set; }
        public List<ReviewQueueSummary> ItemsByType { get; set; } = new();
    }

    public class ReviewQueueSummary
    {
        public string ReviewType { get; set; } = string.Empty;
        public int Count { get; set; }
        public int HighPriorityCount { get; set; }
        public DateTime? OldestItemDate { get; set; }
    }

    public class QuickStatsData
    {
        public List<StatMetric> Metrics { get; set; } = new();
    }

    public class StatMetric
    {
        public string Label { get; set; } = string.Empty;
        public string Value { get; set; } = string.Empty;
        public string Delta { get; set; } = string.Empty;
        public bool IsIncrease { get; set; }
        public string Context { get; set; } = string.Empty;
    }
}
