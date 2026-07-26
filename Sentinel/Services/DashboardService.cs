using Microsoft.EntityFrameworkCore;
using Sentinel.Data;
using Sentinel.Models;
using Sentinel.Models.Dashboard;
using System.Text.Json;

namespace Sentinel.Services
{
    public class DashboardService : IDashboardService
    {
        private readonly ApplicationDbContext _context;

        public DashboardService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<DashboardConfig> GetUserDashboardConfigAsync(string userId)
        {
            var user = await _context.Users.FindAsync(userId);

            if (user?.DashboardConfigJson == null)
            {
                return GetDefaultConfig("User");
            }

            try
            {
                var config = JsonSerializer.Deserialize<DashboardConfig>(user.DashboardConfigJson);
                if (config == null)
                {
                    return GetDefaultConfig("User");
                }

                // Ensure PinnedDiseases is never null
                config.PinnedDiseases ??= new List<string>();
                config.Widgets ??= new List<WidgetConfig>();
                config.TimeDefaults ??= new TimeDefaults();

                return config;
            }
            catch
            {
                return GetDefaultConfig("User");
            }
        }

        public async Task SaveUserDashboardConfigAsync(string userId, DashboardConfig config)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null) return;

            user.DashboardConfigJson = JsonSerializer.Serialize(config);
            await _context.SaveChangesAsync();
        }

        public DashboardConfig GetDefaultConfig(string userRole)
        {
            return new DashboardConfig
            {
                ConfigVersion = 1,
                Layout = "grid",
                TimeDefaults = new TimeDefaults { DefaultTimeWindow = "24h" },
                PinnedDiseases = new List<string>(),
                Widgets = new List<WidgetConfig>
                {
                    new WidgetConfig { WidgetId = "recent-activity", Position = 0, Size = "medium", Settings = new Dictionary<string, object> { { "timeWindow", "24h" } } },
                    new WidgetConfig { WidgetId = "cases-by-disease", Position = 1, Size = "medium", Settings = new Dictionary<string, object> { { "timeWindow", "30d" } } },
                    new WidgetConfig { WidgetId = "hl7-overview", Position = 2, Size = "medium", Settings = new Dictionary<string, object> { { "timeWindow", "24h" } } },
                    new WidgetConfig { WidgetId = "tasks-surveys", Position = 3, Size = "medium", Settings = new Dictionary<string, object> { { "timeWindow", "24h" } } },
                    new WidgetConfig { WidgetId = "outbreak-tracker", Position = 4, Size = "medium", Settings = new Dictionary<string, object>() },
                    new WidgetConfig { WidgetId = "data-review-queue", Position = 5, Size = "medium", Settings = new Dictionary<string, object>() }
                }
            };
        }

        public async Task<WidgetData> GetWidgetDataAsync(string widgetId, string userId, Dictionary<string, object>? settings = null)
        {
            settings ??= new Dictionary<string, object>();

            // Get user's pinned diseases
            var userConfig = await GetUserDashboardConfigAsync(userId);
            var pinnedDiseaseIds = userConfig.PinnedDiseases.Select(id => Guid.Parse(id)).ToList();

            try
            {
                return widgetId switch
                {
                    "recent-activity" => await GetRecentActivityDataAsync(userId, settings, pinnedDiseaseIds),
                    "cases-by-disease" => await GetCasesByDiseaseDataAsync(userId, settings, pinnedDiseaseIds),
                    "hl7-overview" => await GetHL7OverviewDataAsync(userId, settings, pinnedDiseaseIds),
                    "tasks-surveys" => await GetTasksAndSurveysDataAsync(userId, settings, pinnedDiseaseIds),
                    "outbreak-tracker" => await GetOutbreakTrackerDataAsync(userId, settings, pinnedDiseaseIds),
                    "data-review-queue" => await GetDataReviewQueueDataAsync(userId, settings, pinnedDiseaseIds),
                    "quick-stats" => await GetQuickStatsDataAsync(userId, settings, pinnedDiseaseIds),
                    _ => new WidgetData { WidgetId = widgetId, Title = "Unknown Widget", ErrorMessage = "Widget not found" }
                };
            }
            catch (Exception ex)
            {
                return new WidgetData { WidgetId = widgetId, Title = "Error", ErrorMessage = ex.Message };
            }
        }

        private async Task<WidgetData> GetRecentActivityDataAsync(string userId, Dictionary<string, object> settings, List<Guid> pinnedDiseaseIds)
        {
            var timeWindow = settings.ContainsKey("timeWindow") ? settings["timeWindow"]?.ToString() : "24h";
            var cutoffTime = GetCutoffTime(timeWindow ?? "24h");

            var activities = new List<ActivityItem>();

            // 1. Confirmation Status Changes - ONLY actual changes (not initial set)
            var statusChanges = await _context.AuditLogs
                .Where(a => a.EntityType == "Case" 
                    && a.FieldName == "ConfirmationStatusId"
                    && a.OldValue != null
                    && a.ChangedAt >= cutoffTime)
                .OrderByDescending(a => a.ChangedAt)
                .Take(10)
                .ToListAsync();

            foreach (var change in statusChanges)
            {
                var caseEntity = await _context.Cases
                    .Include(c => c.Disease)
                    .Include(c => c.ConfirmationStatus)
                    .FirstOrDefaultAsync(c => c.Id.ToString() == change.EntityId);

                if (caseEntity != null && !caseEntity.IsDeleted)
                {
                    // Filter by pinned diseases if any are selected
                    if (pinnedDiseaseIds.Any() && caseEntity.DiseaseId.HasValue && !pinnedDiseaseIds.Contains(caseEntity.DiseaseId.Value))
                    {
                        continue;
                    }

                    activities.Add(new ActivityItem
                    {
                        ActivityType = "Status",
                        EntityType = "Case",
                        EntityId = change.EntityId ?? "",
                        DisplayText = $"Case confirmation changed to {caseEntity.ConfirmationStatus?.Name ?? "Unknown"} - {caseEntity.Disease?.Name}",
                        Status = caseEntity.ConfirmationStatus?.Name?.ToLower() ?? "info",
                        OccurredAt = change.ChangedAt,
                        DiseaseCode = caseEntity.Disease?.Code
                    });
                }
            }

            // 2. New Contacts per Disease (grouped summary) - within time window
            // Get contact creation audit logs within the time window
            var contactCreationAuditsList = await _context.AuditLogs
                .Where(a => a.EntityType == "Case" 
                    && a.Action == "Created"
                    && a.ChangedAt >= cutoffTime)
                .ToListAsync();

            var contactCreationAudits = contactCreationAuditsList.ToDictionary(a => a.EntityId, a => a.ChangedAt);

            if (contactCreationAudits.Any())
            {
                var contactIds = contactCreationAudits.Keys.ToList();

                var newContactsQuery = _context.Cases
                    .Include(c => c.Disease)
                    .Where(c => !c.IsDeleted 
                        && c.Type == CaseType.Contact 
                        && c.DiseaseId.HasValue
                        && contactIds.Contains(c.Id.ToString()));

                // Apply disease filter if any pinned
                if (pinnedDiseaseIds.Any())
                {
                    newContactsQuery = newContactsQuery.Where(c => pinnedDiseaseIds.Contains(c.DiseaseId.Value));
                }

                var newContacts = await newContactsQuery.ToListAsync();

                // Create contacts with their creation times
                var contactsWithCreationTime = newContacts
                    .Where(c => contactCreationAudits.ContainsKey(c.Id.ToString()))
                    .Select(c => new 
                    { 
                        Contact = c, 
                        CreatedAt = contactCreationAudits[c.Id.ToString()] 
                    })
                    .ToList();

                var contactsByDisease = contactsWithCreationTime
                    .GroupBy(c => new { c.Contact.DiseaseId, DiseaseName = c.Contact.Disease?.Name, DiseaseCode = c.Contact.Disease?.Code })
                    .Select(g => new
                    {
                        g.Key.DiseaseId,
                        g.Key.DiseaseName,
                        g.Key.DiseaseCode,
                        Count = g.Count(),
                        MostRecentCreation = g.Max(x => x.CreatedAt)
                    })
                    .Where(x => x.Count > 0)
                    .OrderByDescending(x => x.Count)
                    .Take(5);

                foreach (var group in contactsByDisease)
                {
                    activities.Add(new ActivityItem
                    {
                        ActivityType = "Contacts",
                        EntityType = "Disease",
                        EntityId = group.DiseaseId?.ToString() ?? "",
                        DisplayText = $"{group.Count} new contact{(group.Count != 1 ? "s" : "")} - {group.DiseaseName}",
                        Status = "info",
                        OccurredAt = group.MostRecentCreation,
                        DiseaseCode = group.DiseaseCode
                    });
                }
            }

            // 3. New Outbreaks - within time window
            var newOutbreaks = await _context.Outbreaks
                .Include(o => o.PrimaryDisease)
                .Include(o => o.OutbreakCases)
                .Where(o => !o.IsDeleted 
                    && o.Status == OutbreakStatus.Active
                    && o.StartDate >= cutoffTime)
                .OrderByDescending(o => o.StartDate)
                .Take(5)
                .ToListAsync();

            foreach (var outbreak in newOutbreaks)
            {
                var linkedCases = outbreak.OutbreakCases.Count;

                activities.Add(new ActivityItem
                {
                    ActivityType = "Outbreak",
                    EntityType = "Outbreak",
                    EntityId = outbreak.Id.ToString(),
                    DisplayText = $"Outbreak: {outbreak.Name} ({linkedCases} linked case{(linkedCases != 1 ? "s" : "")})",
                    Status = "outbreak",
                    OccurredAt = outbreak.StartDate,
                    DiseaseCode = outbreak.PrimaryDisease?.Code
                });
            }

            // 4. Disease Type Changes - ONLY actual changes (not initial set)
            var diseaseChanges = await _context.AuditLogs
                .Where(a => a.EntityType == "Case" 
                    && a.FieldName == "DiseaseId"
                    && a.OldValue != null
                    && a.ChangedAt >= cutoffTime)
                .OrderByDescending(a => a.ChangedAt)
                .Take(10)
                .ToListAsync();

            foreach (var change in diseaseChanges)
            {
                var caseEntity = await _context.Cases
                    .Include(c => c.Disease)
                    .FirstOrDefaultAsync(c => c.Id.ToString() == change.EntityId);

                if (caseEntity != null && !caseEntity.IsDeleted)
                {
                    activities.Add(new ActivityItem
                    {
                        ActivityType = "Disease",
                        EntityType = "Case",
                        EntityId = change.EntityId ?? "",
                        DisplayText = $"Case disease changed to {caseEntity.Disease?.Name ?? "Unknown"}",
                        Status = "watch",
                        OccurredAt = change.ChangedAt,
                        DiseaseCode = caseEntity.Disease?.Code
                    });
                }
            }

            // Sort all activities by time and take top items
            var sortedActivities = activities
                .OrderByDescending(a => a.OccurredAt)
                .Take(10)
                .ToList();

            return new WidgetData
            {
                WidgetId = "recent-activity",
                Title = "Recent Activity",
                Data = new RecentActivityData 
                { 
                    Items = sortedActivities,
                    TotalCount = sortedActivities.Count
                },
                LastUpdated = DateTime.UtcNow
            };
        }

        private async Task<WidgetData> GetCasesByDiseaseDataAsync(string userId, Dictionary<string, object> settings, List<Guid> pinnedDiseaseIds)
        {
            var timeWindow = settings.ContainsKey("timeWindow") ? settings["timeWindow"]?.ToString() : "30d";
            var cutoffTime = GetCutoffTime(timeWindow ?? "30d");

            // Query Cases directly using DateOfNotification instead of relying on audit logs
            var casesQuery = _context.Cases
                .Include(c => c.Disease)
                .Include(c => c.ConfirmationStatus)
                .Where(c => !c.IsDeleted 
                    && c.Type == CaseType.Case 
                    && c.DiseaseId.HasValue
                    && c.DateOfNotification.HasValue
                    && c.DateOfNotification >= cutoffTime);

            // Filter by pinned diseases if any
            if (pinnedDiseaseIds.Any())
            {
                casesQuery = casesQuery.Where(c => pinnedDiseaseIds.Contains(c.DiseaseId.Value));
            }

            var cases = await casesQuery.ToListAsync();

            var diseaseGroups = cases
                .GroupBy(c => new { c.DiseaseId, DiseaseName = c.Disease?.Name ?? "Unknown" })
                .Select(g => new DiseaseCount
                {
                    DiseaseId = g.Key.DiseaseId!.Value,
                    DiseaseName = g.Key.DiseaseName,
                    ConfirmedCount = g.Count(c => c.ConfirmationStatus?.Name == "Confirmed"),
                    ProbableCount = g.Count(c => c.ConfirmationStatus?.Name == "Probable"),
                    TotalCount = g.Count(),
                    Delta7Day = 0,
                    StatusColor = g.Any(c => c.ConfirmationStatus?.Name == "Confirmed") ? "outbreak" : "clear"
                })
                .OrderByDescending(d => d.TotalCount)
                .Take(10)
                .ToList();

            return new WidgetData
            {
                WidgetId = "cases-by-disease",
                Title = "Cases by Disease",
                Data = new CasesByDiseaseData { Diseases = diseaseGroups },
                LastUpdated = DateTime.UtcNow
            };
        }

        private async Task<WidgetData> GetHL7OverviewDataAsync(string userId, Dictionary<string, object> settings, List<Guid> pinnedDiseaseIds)
        {
            var timeWindow = settings.ContainsKey("timeWindow") ? settings["timeWindow"]?.ToString() : "24h";
            var cutoffTime = GetCutoffTime(timeWindow ?? "24h");

            var messages = await _context.HL7Messages
                .Where(m => !m.IsDeleted && m.ReceivedAt >= cutoffTime)
                .ToListAsync();

            var processed = messages.Count(m => m.Status == HL7ProcessingStatus.ProcessedSuccessfully || m.Status == HL7ProcessingStatus.ProcessedWithWarnings);
            var rejected = messages.Count(m => m.Status == HL7ProcessingStatus.ParsingFailed || m.Status == HL7ProcessingStatus.ProcessingFailed);
            var awaitingReview = messages.Count(m => m.RequiresManualReview && !m.ManualReviewCompleted);

            var rejectionRate = messages.Any() ? (double)rejected / messages.Count * 100 : 0;
            var healthStatus = rejectionRate > 10 ? "error" : rejectionRate > 5 ? "warning" : "healthy";

            return new WidgetData
            {
                WidgetId = "hl7-overview",
                Title = "HL7 Processing",
                Data = new HL7OverviewData
                {
                    ProcessedCount = processed,
                    RejectedCount = rejected,
                    AwaitingReviewCount = awaitingReview,
                    AvgProcessingTimeMs = 0,
                    RejectionRate = rejectionRate,
                    HealthStatus = healthStatus
                },
                LastUpdated = DateTime.UtcNow
            };
        }

        private async Task<WidgetData> GetTasksAndSurveysDataAsync(string userId, Dictionary<string, object> settings, List<Guid> pinnedDiseaseIds)
        {
            var timeWindow = settings.ContainsKey("timeWindow") ? settings["timeWindow"]?.ToString() : "24h";
            var cutoffTime = GetCutoffTime(timeWindow ?? "24h");

            // Get all tasks for outstanding/overdue counts (not filtered by time)
            var allTasks = await _context.CaseTasks
                .Include(t => t.TaskType)
                .ToListAsync();

            // Get tasks created in time window for "new tasks" metric
            var recentTasks = allTasks.Where(t => t.CreatedAt >= cutoffTime).ToList();

            var outstanding = allTasks.Count(t => t.Status != CaseTaskStatus.Completed && t.Status != CaseTaskStatus.Cancelled);
            var completedInWindow = recentTasks.Count(t => t.Status == CaseTaskStatus.Completed);
            var overdue = allTasks.Count(t => t.Status != CaseTaskStatus.Completed && t.DueDate.HasValue && t.DueDate < DateTime.UtcNow);

            var taskTypeSummary = allTasks
                .Where(t => t.Status != CaseTaskStatus.Completed)
                .GroupBy(t => t.TaskType?.Name ?? "Unspecified")
                .Select(g => new TaskSummary
                {
                    TaskType = g.Key,
                    Count = g.Count(),
                    OverdueCount = g.Count(t => t.DueDate.HasValue && t.DueDate < DateTime.UtcNow)
                })
                .OrderByDescending(t => t.Count)
                .Take(5)
                .ToList();

            return new WidgetData
            {
                WidgetId = "tasks-surveys",
                Title = "Tasks & Surveys",
                Data = new TasksAndSurveysData
                {
                    OutstandingTasks = outstanding,
                    CompletedToday = completedInWindow,
                    OverdueTasks = overdue,
                    TopTaskTypes = taskTypeSummary
                },
                LastUpdated = DateTime.UtcNow
            };
        }

        private async Task<WidgetData> GetOutbreakTrackerDataAsync(string userId, Dictionary<string, object> settings, List<Guid> pinnedDiseaseIds)
        {
            var outbreaks = await _context.Outbreaks
                .Include(o => o.OutbreakCases)
                .Where(o => !o.IsDeleted && o.Status == OutbreakStatus.Active)
                .ToListAsync();

            var summaries = new List<OutbreakSummary>();

            foreach (var outbreak in outbreaks)
            {
                var teamCount = await _context.OutbreakTeamMembers
                    .Where(tm => tm.OutbreakId == outbreak.Id)
                    .CountAsync();

                summaries.Add(new OutbreakSummary
                {
                    OutbreakId = Guid.NewGuid(), // Convert from int
                    OutbreakName = outbreak.Name,
                    LinkedCasesCount = outbreak.OutbreakCases?.Count ?? 0,
                    DaysSinceDeclaration = (DateTime.UtcNow - outbreak.StartDate).Days,
                    TeamMemberCount = teamCount,
                    LatestActivity = "Active investigation",
                    LatestActivityTime = outbreak.StartDate,
                    IsNew = outbreak.StartDate >= DateTime.UtcNow.AddHours(-24)
                });
            }

            return new WidgetData
            {
                WidgetId = "outbreak-tracker",
                Title = "Outbreak Tracker",
                Data = new OutbreakTrackerData { ActiveOutbreaks = summaries },
                LastUpdated = DateTime.UtcNow
            };
        }

        private async Task<WidgetData> GetDataReviewQueueDataAsync(string userId, Dictionary<string, object> settings, List<Guid> pinnedDiseaseIds)
        {
            var items = await _context.ReviewQueue
                .Where(r => r.ReviewStatus == "Pending")
                .ToListAsync();

            var byType = items
                .GroupBy(r => r.ChangeType ?? "Unknown")
                .Select(g => new ReviewQueueSummary
                {
                    ReviewType = g.Key,
                    Count = g.Count(),
                    HighPriorityCount = g.Count(r => r.Priority > 5),
                    OldestItemDate = g.Min(r => r.CreatedDate)
                })
                .OrderByDescending(r => r.Count)
                .ToList();

            return new WidgetData
            {
                WidgetId = "data-review-queue",
                Title = "Data Review Queue",
                Data = new DataReviewQueueData
                {
                    TotalItems = items.Count,
                    ItemsByType = byType
                },
                LastUpdated = DateTime.UtcNow
            };
        }

        private async Task<WidgetData> GetQuickStatsDataAsync(string userId, Dictionary<string, object> settings, List<Guid> pinnedDiseaseIds)
        {
            var metrics = new List<StatMetric>();

            var totalCases = await _context.Cases
                .Where(c => !c.IsDeleted && c.Type == CaseType.Case)
                .CountAsync();

            metrics.Add(new StatMetric
            {
                Label = "Total Active Cases",
                Value = totalCases.ToString(),
                Delta = "",
                IsIncrease = false,
                Context = ""
            });

            return new WidgetData
            {
                WidgetId = "quick-stats",
                Title = "Quick Stats",
                Data = new QuickStatsData { Metrics = metrics },
                LastUpdated = DateTime.UtcNow
            };
        }

        private DateTime GetCutoffTime(string timeWindow)
        {
            return timeWindow switch
            {
                "24h" => DateTime.UtcNow.AddHours(-24),
                "48h" => DateTime.UtcNow.AddHours(-48),
                "7d" => DateTime.UtcNow.AddDays(-7),
                "30d" => DateTime.UtcNow.AddDays(-30),
                "thisWeek" => DateTime.UtcNow.Date.AddDays(-(int)DateTime.UtcNow.DayOfWeek),
                "thisMonth" => new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1),
                _ => DateTime.UtcNow.AddHours(-24)
            };
        }
    }
}
