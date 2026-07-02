using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Sentinel.Services;
using Sentinel.Models.Dashboard;
using System.Security.Claims;
using System.Text.Json;
using Sentinel.Data;
using Microsoft.EntityFrameworkCore;
using Sentinel.Models;

namespace Sentinel.Pages
{
    [IgnoreAntiforgeryToken]
    public class DashboardModel : PageModel
    {
        private readonly IDashboardService _dashboardService;
        private readonly ApplicationDbContext _context;

        public DashboardModel(IDashboardService dashboardService, ApplicationDbContext context)
        {
            _dashboardService = dashboardService;
            _context = context;
        }

        public DashboardConfig Config { get; set; } = new();
        public Dictionary<string, WidgetData> WidgetDataCache { get; set; } = new();
        public string UserDisplayName { get; set; } = string.Empty;
        public List<DiseaseOption> AvailableDiseases { get; set; } = new();

        // Personalized dashboard data
        public List<RecentlyViewedItem> RecentlyViewed { get; set; } = new();
        public List<OutbreakSummaryItem> MyOutbreaks { get; set; } = new();
        public List<TaskSummaryItem> MyTasks { get; set; } = new();

        public async Task OnGetAsync()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null) return;

            // Get user's display name
            var firstName = User.FindFirstValue(ClaimTypes.GivenName);
            var email = User.FindFirstValue(ClaimTypes.Email);

            if (!string.IsNullOrWhiteSpace(firstName))
            {
                UserDisplayName = firstName;
            }
            else if (!string.IsNullOrWhiteSpace(email))
            {
                UserDisplayName = email.Split('@')[0];
            }

            // Load user's dashboard configuration
            Config = await _dashboardService.GetUserDashboardConfigAsync(userId);

            // Load available diseases for filter
            AvailableDiseases = await _context.Diseases
                .OrderBy(d => d.Name)
                .Select(d => new DiseaseOption 
                { 
                    Id = d.Id, 
                    Name = d.Name ?? "Unknown",
                    Code = d.Code,
                    IsPinned = Config.PinnedDiseases.Contains(d.Id.ToString())
                })
                .ToListAsync();

            // Load personalized dashboard data
            await LoadPersonalizedDataAsync(userId);

            // Load data for each widget
            foreach (var widget in Config.Widgets.OrderBy(w => w.Position))
            {
                var data = await _dashboardService.GetWidgetDataAsync(widget.WidgetId, userId, widget.Settings);
                WidgetDataCache[widget.WidgetId] = data;
            }
        }

        private async Task LoadPersonalizedDataAsync(string userId)
        {
            // Load recently viewed items from audit logs
            var recentViews = await _context.AuditLogs
                .Where(a => a.ChangedByUserId == userId && 
                           a.Action == "Viewed" && 
                           a.ChangedAt >= DateTime.UtcNow.AddDays(-7))
                .OrderByDescending(a => a.ChangedAt)
                .Take(20)
                .ToListAsync();

            // Parse and map to view model with actual names
            var viewedItems = new List<RecentlyViewedItem>();
            foreach (var log in recentViews)
            {
                if (Guid.TryParse(log.EntityId, out var entityId))
                {
                    string displayName = "Unknown";
                    string subText = "";

                    // Try to get the actual record name
                    try
                    {
                        switch (log.EntityType)
                        {
                            case "Patient":
                                var patient = await _context.Patients
                                    .Where(p => p.Id == entityId && !p.IsDeleted)
                                    .Select(p => new { p.GivenName, p.FamilyName, p.DateOfBirth })
                                    .FirstOrDefaultAsync();
                                if (patient != null)
                                {
                                    displayName = $"{patient.GivenName} {patient.FamilyName}";
                                    subText = patient.DateOfBirth.HasValue ? patient.DateOfBirth.Value.ToString("dd/MM/yyyy") : "";
                                }
                                break;

                            case "Case":
                                var caseRecord = await _context.Cases
                                    .Include(c => c.Patient)
                                    .Include(c => c.Disease)
                                    .Include(c => c.ConfirmationStatus)
                                    .Where(c => c.Id == entityId && !c.IsDeleted)
                                    .FirstOrDefaultAsync();
                                if (caseRecord?.Patient != null)
                                {
                                    displayName = $"{caseRecord.Patient.GivenName} {caseRecord.Patient.FamilyName}";
                                    var diseaseName = caseRecord.Disease?.Name ?? "Unknown Disease";
                                    var statusName = caseRecord.ConfirmationStatus?.Name ?? "Unknown Status";
                                    subText = $"{diseaseName} · {statusName}";
                                }
                                break;

                            case "Contact":
                                var contact = await _context.Cases
                                    .Include(c => c.Patient)
                                    .Include(c => c.Disease)
                                    .Include(c => c.ConfirmationStatus)
                                    .Where(c => c.Id == entityId && !c.IsDeleted && c.Type == CaseType.Contact)
                                    .FirstOrDefaultAsync();
                                if (contact?.Patient != null)
                                {
                                    displayName = $"{contact.Patient.GivenName} {contact.Patient.FamilyName}";
                                    var diseaseName = contact.Disease?.Name ?? "Unknown Disease";
                                    var statusName = contact.ConfirmationStatus?.Name ?? "Unknown Status";
                                    subText = $"{diseaseName} · {statusName}";
                                }
                                break;
                        }
                    }
                    catch { /* Skip if record no longer exists */ }

                    if (displayName != "Unknown")
                    {
                        viewedItems.Add(new RecentlyViewedItem
                        {
                            EntityId = entityId,
                            EntityType = log.EntityType ?? "Unknown",
                            DisplayName = displayName,
                            SubText = subText,
                            ViewedAt = log.ChangedAt
                        });
                    }
                }
            }

            // Deduplicate by EntityId, keeping most recent
            RecentlyViewed = viewedItems
                .GroupBy(r => r.EntityId)
                .Select(g => g.First())
                .Take(5)
                .ToList();

            // Load user's outbreaks (where they are team member or lead investigator)
            MyOutbreaks = await _context.Outbreaks
                .Where(o => !o.IsDeleted && 
                           (o.Status == OutbreakStatus.Active || o.Status == OutbreakStatus.Monitoring) &&
                           (o.LeadInvestigatorId == userId || 
                            o.TeamMembers.Any(tm => tm.UserId == userId)))
                .OrderByDescending(o => o.StartDate)
                .Select(o => new OutbreakSummaryItem
                {
                    Id = o.Id,
                    Name = o.Name,
                    Status = o.Status.ToString(),
                    DiseaseName = o.PrimaryDisease != null ? o.PrimaryDisease.Name : null
                })
                .Take(5)
                .ToListAsync();

            // Load user's assigned tasks
            MyTasks = await _context.CaseTasks
                .Where(t => t.AssignedToUserId == userId && 
                           t.CompletedAt == null &&
                           t.CancelledAt == null)
                .OrderByDescending(t => t.Priority)
                .ThenBy(t => t.DueDate)
                .Select(t => new TaskSummaryItem
                {
                    Id = t.Id,
                    CaseId = t.CaseId,
                    Title = t.Title,
                    Priority = t.Priority.ToString(),
                    PatientName = t.Case != null && t.Case.Patient != null 
                        ? (t.Case.Patient.GivenName + " " + t.Case.Patient.FamilyName) 
                        : null
                })
                .Take(5)
                .ToListAsync();
        }

        public async Task<IActionResult> OnPostRefreshWidgetAsync([FromBody] RefreshWidgetRequest request)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null) return Unauthorized();

            // Load current config to get widget settings
            var config = await _dashboardService.GetUserDashboardConfigAsync(userId);
            var widget = config.Widgets.FirstOrDefault(w => w.WidgetId == request.WidgetId);

            var settings = widget?.Settings ?? new Dictionary<string, object>();

            // Merge any new settings from request
            if (request.Settings != null)
            {
                foreach (var kvp in request.Settings)
                {
                    settings[kvp.Key] = kvp.Value;
                }
            }

            var data = await _dashboardService.GetWidgetDataAsync(request.WidgetId, userId, settings);
            return new JsonResult(data);
        }

        public async Task<IActionResult> OnPostUpdateWidgetSettingsAsync([FromBody] UpdateWidgetSettingsRequest request)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null) return Unauthorized();

            var config = await _dashboardService.GetUserDashboardConfigAsync(userId);
            var widget = config.Widgets.FirstOrDefault(w => w.WidgetId == request.WidgetId);

            if (widget == null) return NotFound();

            // Update settings
            foreach (var kvp in request.Settings)
            {
                widget.Settings[kvp.Key] = kvp.Value;
            }

            await _dashboardService.SaveUserDashboardConfigAsync(userId, config);

            // Return fresh data with new settings
            var data = await _dashboardService.GetWidgetDataAsync(request.WidgetId, userId, widget.Settings);
            return new JsonResult(new { success = true, data });
        }

        public async Task<IActionResult> OnPostSaveConfigAsync([FromBody] DashboardConfig config)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null) return Unauthorized();

            await _dashboardService.SaveUserDashboardConfigAsync(userId, config);
            return new JsonResult(new { success = true });
        }

        public async Task<IActionResult> OnGetResetConfigAsync()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null) return Unauthorized();

            var defaultConfig = _dashboardService.GetDefaultConfig("User");
            await _dashboardService.SaveUserDashboardConfigAsync(userId, defaultConfig);

            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostTogglePinnedDiseaseAsync([FromBody] ToggleDiseaseRequest? request)
        {
            if (request == null)
            {
                return BadRequest(new { error = "Invalid request" });
            }

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null) return Unauthorized();

            var config = await _dashboardService.GetUserDashboardConfigAsync(userId);

            if (config == null || config.PinnedDiseases == null)
            {
                return BadRequest(new { error = "Invalid configuration" });
            }

            var diseaseIdString = request.DiseaseId.ToString();

            if (request.IsPinned)
            {
                // Add to pinned list
                if (!config.PinnedDiseases.Contains(diseaseIdString))
                {
                    config.PinnedDiseases.Add(diseaseIdString);
                }
            }
            else
            {
                // Remove from pinned list
                config.PinnedDiseases.Remove(diseaseIdString);
            }

            await _dashboardService.SaveUserDashboardConfigAsync(userId, config);

            // Refresh all widgets to apply disease filter
            return new JsonResult(new { success = true, refreshWidgets = true });
        }

        public async Task<IActionResult> OnGetQuickSearchAsync(string q)
        {
            if (string.IsNullOrWhiteSpace(q) || q.Length < 2)
            {
                return new JsonResult(new { results = new List<object>() });
            }

            var searchTerm = q.Trim().ToLower();
            var results = new List<QuickSearchResult>();

            // Search Patients
            var patients = await _context.Patients
                .Where(p => !p.IsDeleted && 
                           (p.GivenName.ToLower().Contains(searchTerm) ||
                            p.FamilyName.ToLower().Contains(searchTerm) ||
                            (p.HomePhone != null && p.HomePhone.Contains(searchTerm)) ||
                            (p.MobilePhone != null && p.MobilePhone.Contains(searchTerm)) ||
                            (p.EmailAddress != null && p.EmailAddress.ToLower().Contains(searchTerm)) ||
                            (p.AddressLine != null && p.AddressLine.ToLower().Contains(searchTerm)) ||
                            (p.City != null && p.City.ToLower().Contains(searchTerm))))
                .Take(5)
                .Select(p => new QuickSearchResult
                {
                    Type = "Patient",
                    Id = p.Id.ToString(),
                    Title = p.GivenName + " " + p.FamilyName,
                    Subtitle = p.DateOfBirth.HasValue ? p.DateOfBirth.Value.ToString("dd/MM/yyyy") : "",
                    Icon = "person",
                    Url = $"/Patients/Details?id={p.Id}"
                })
                .ToListAsync();
            results.AddRange(patients);

            // Search Cases
            var cases = await _context.Cases
                .Include(c => c.Patient)
                .Include(c => c.Disease)
                .Where(c => !c.IsDeleted &&
                           c.Patient != null && 
                           (c.Patient.GivenName.ToLower().Contains(searchTerm) ||
                            c.Patient.FamilyName.ToLower().Contains(searchTerm) ||
                            (c.Patient.City != null && c.Patient.City.ToLower().Contains(searchTerm))))
                .Take(5)
                .Select(c => new QuickSearchResult
                {
                    Type = "Case",
                    Id = c.Id.ToString(),
                    Title = c.Patient != null ? (c.Patient.GivenName + " " + c.Patient.FamilyName) : "Unknown",
                    Subtitle = c.Disease != null ? c.Disease.Name : "Unknown Disease",
                    Icon = "file-medical",
                    Url = $"/Cases/Details?id={c.Id}"
                })
                .ToListAsync();
            results.AddRange(cases);

            // Search Contacts (cases with Type = Contact)
            var contacts = await _context.Cases
                .Include(c => c.Patient)
                .Where(c => !c.IsDeleted &&
                           c.Type == CaseType.Contact &&
                           c.Patient != null &&
                           (c.Patient.GivenName.ToLower().Contains(searchTerm) ||
                            c.Patient.FamilyName.ToLower().Contains(searchTerm) ||
                            (c.Patient.AddressLine != null && c.Patient.AddressLine.ToLower().Contains(searchTerm)) ||
                            (c.Patient.City != null && c.Patient.City.ToLower().Contains(searchTerm))))
                .Take(5)
                .Select(c => new QuickSearchResult
                {
                    Type = "Contact",
                    Id = c.Id.ToString(),
                    Title = c.Patient != null ? (c.Patient.GivenName + " " + c.Patient.FamilyName) : "Unknown",
                    Subtitle = c.Patient != null && c.Patient.DateOfBirth.HasValue ? c.Patient.DateOfBirth.Value.ToString("dd/MM/yyyy") : "",
                    Icon = "people",
                    Url = $"/Contacts/Details?id={c.Id}"
                })
                .ToListAsync();
            results.AddRange(contacts);

            // Search Outbreaks
            var outbreaks = await _context.Outbreaks
                .Include(o => o.PrimaryDisease)
                .Include(o => o.PrimaryLocation)
                .Where(o => !o.IsDeleted &&
                           (o.Name.ToLower().Contains(searchTerm) ||
                            (o.Description != null && o.Description.ToLower().Contains(searchTerm)) ||
                            (o.PrimaryLocation != null && o.PrimaryLocation.Address != null && o.PrimaryLocation.Address.ToLower().Contains(searchTerm))))
                .Take(5)
                .Select(o => new QuickSearchResult
                {
                    Type = "Outbreak",
                    Id = o.Id.ToString(),
                    Title = o.Name,
                    Subtitle = o.PrimaryDisease != null ? o.PrimaryDisease.Name : "",
                    Icon = "diagram-3",
                    Url = $"/Outbreaks/Details?id={o.Id}"
                })
                .ToListAsync();
            results.AddRange(outbreaks);

            return new JsonResult(new { results = results.Take(15) });
        }
    }

    public class DiseaseOption
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Code { get; set; }
        public bool IsPinned { get; set; }
    }

    public class RefreshWidgetRequest
    {
        public string WidgetId { get; set; } = string.Empty;
        public Dictionary<string, object>? Settings { get; set; }
    }

    public class UpdateWidgetSettingsRequest
    {
        public string WidgetId { get; set; } = string.Empty;
        public Dictionary<string, object> Settings { get; set; } = new();
    }

    public class ToggleDiseaseRequest
    {
        public Guid DiseaseId { get; set; }
        public bool IsPinned { get; set; }
    }

    public class RecentlyViewedItem
    {
        public Guid EntityId { get; set; }
        public string EntityType { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public string SubText { get; set; } = string.Empty;
        public DateTime ViewedAt { get; set; }
    }

    public class OutbreakSummaryItem
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string? DiseaseName { get; set; }
    }

    public class TaskSummaryItem
    {
        public Guid Id { get; set; }
        public Guid CaseId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Priority { get; set; } = string.Empty;
        public string? PatientName { get; set; }
    }

    public class QuickSearchResult
    {
        public string Type { get; set; } = string.Empty;
        public string Id { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Subtitle { get; set; } = string.Empty;
        public string Icon { get; set; } = string.Empty;
        public string Url { get; set; } = string.Empty;
    }
}
