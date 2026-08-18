using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Authorization;
using Sentinel.Services;
using Sentinel.Models.Dashboard;
using System.Security.Claims;
using System.Text.Json;
using Sentinel.Data;
using Microsoft.EntityFrameworkCore;
using Sentinel.Models;

namespace Sentinel.Pages
{
    [Authorize]
    [IgnoreAntiforgeryToken]
    public class DashboardModel : PageModel
    {
        private static readonly string[] DashboardPermissionKeys =
        [
            "Patient.View",
            "Patient.Create",
            "Case.View",
            "Case.Create",
            "Task.View",
            "Outbreak.View",
            "Report.Edit",
            "HL7.View"
        ];

        private readonly IDashboardService _dashboardService;
        private readonly ApplicationDbContext _context;
        private readonly IPermissionService _permissionService;

        public DashboardModel(
            IDashboardService dashboardService,
            ApplicationDbContext context,
            IPermissionService permissionService)
        {
            _dashboardService = dashboardService;
            _context = context;
            _permissionService = permissionService;
        }

        public DashboardConfig Config { get; set; } = new();
        public Dictionary<string, WidgetData> WidgetDataCache { get; set; } = new();
        public string UserDisplayName { get; set; } = string.Empty;
        public List<DiseaseOption> AvailableDiseases { get; set; } = new();
        public HashSet<string> PermissionKeys { get; private set; } = new(StringComparer.OrdinalIgnoreCase);

        public bool HasPermission(string permissionKey) => PermissionKeys.Contains(permissionKey);

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

            foreach (var permissionKey in DashboardPermissionKeys)
            {
                if (await _permissionService.HasPermissionAsync(userId, permissionKey))
                {
                    PermissionKeys.Add(permissionKey);
                }
            }

            // Load available diseases for filter
            if (HasPermission("Case.View"))
            {
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
            }

            // Load personalized dashboard data
            await LoadPersonalizedDataAsync(userId);

            // Load data for each widget
            var permittedWidgets = new List<WidgetConfig>();
            foreach (var widget in Config.Widgets.OrderBy(w => w.Position))
            {
                if (!await _dashboardService.CanAccessWidgetAsync(widget.WidgetId, userId))
                {
                    continue;
                }

                var data = await _dashboardService.GetWidgetDataAsync(widget.WidgetId, userId, widget.Settings);
                WidgetDataCache[widget.WidgetId] = data;
                permittedWidgets.Add(widget);
            }

            Config.Widgets = permittedWidgets;
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
                                if (!HasPermission("Patient.View")) break;
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
                                if (!HasPermission("Case.View")) break;
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
                                if (!HasPermission("Case.View")) break;
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
            if (HasPermission("Outbreak.View"))
            {
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
            }

            // Load user's assigned tasks
            if (HasPermission("Task.View") && HasPermission("Case.View"))
            {
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
        }

        public async Task<IActionResult> OnPostRefreshWidgetAsync([FromBody] RefreshWidgetRequest request)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null) return Unauthorized();

            if (request == null || string.IsNullOrEmpty(request.WidgetId))
                return BadRequest("Invalid widget request");

            if (!await _dashboardService.CanAccessWidgetAsync(request.WidgetId, userId))
                return Forbid();

            // Load current config to get widget settings
            var config = await _dashboardService.GetUserDashboardConfigAsync(userId);
            if (config == null || config.Widgets == null) return BadRequest("Dashboard configuration not found");

            var widget = config.Widgets
                .Where(w => w != null && !string.IsNullOrEmpty(w.WidgetId))
                .FirstOrDefault(w => w.WidgetId == request.WidgetId);

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

            if (request == null || string.IsNullOrEmpty(request.WidgetId))
                return BadRequest("Invalid widget request");

            if (!await _dashboardService.CanAccessWidgetAsync(request.WidgetId, userId))
                return Forbid();

            var config = await _dashboardService.GetUserDashboardConfigAsync(userId);
            if (config == null || config.Widgets == null) return BadRequest("Dashboard configuration not found");

            var widget = config.Widgets
                .Where(w => w != null && !string.IsNullOrEmpty(w.WidgetId))
                .FirstOrDefault(w => w.WidgetId == request.WidgetId);

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

            if (!await _permissionService.HasPermissionAsync(userId, "Case.View"))
                return Forbid();

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

            if (!await _permissionService.HasPermissionAsync(userId, "Case.View"))
                return Forbid();

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

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(userId))
            {
                return Unauthorized();
            }

            // Quick search must not disclose an entity merely because the dashboard is
            // available. Check effective permissions individually so an explicit user
            // denial correctly overrides an otherwise-granted role permission.
            var canViewPatients = await _permissionService.HasPermissionAsync(userId, "Patient.View");
            var canViewCases = await _permissionService.HasPermissionAsync(userId, "Case.View");
            var canViewOutbreaks = await _permissionService.HasPermissionAsync(userId, "Outbreak.View");
            var canViewEvents = await _permissionService.HasPermissionAsync(userId, "Event.View");
            var canViewLocations = await _permissionService.HasPermissionAsync(userId, "Location.View");
            // Organisations are managed through Settings and their pages use Settings.View.
            var canViewOrganizations = await _permissionService.HasPermissionAsync(userId, "Settings.View");

            var searchTerm = q.Trim().ToLower();
            var results = new List<QuickSearchResult>();

            // Try to parse as GUID for ID searches
            Guid guidSearch;
            bool isGuidSearch = Guid.TryParse(searchTerm, out guidSearch);

            // The patient query inherits the configurable case-scoped patient-access filter.
            if (canViewPatients)
            {
                var patients = await _context.Patients
                    .Where(p => !p.IsDeleted &&
                               (p.GivenName.ToLower().Contains(searchTerm) ||
                                p.FamilyName.ToLower().Contains(searchTerm) ||
                                (p.HomePhone != null && p.HomePhone.Contains(searchTerm)) ||
                                (p.MobilePhone != null && p.MobilePhone.Contains(searchTerm)) ||
                                (p.EmailAddress != null && p.EmailAddress.ToLower().Contains(searchTerm)) ||
                                (p.AddressLine != null && p.AddressLine.ToLower().Contains(searchTerm)) ||
                                (p.City != null && p.City.ToLower().Contains(searchTerm)) ||
                                (p.FriendlyId != null && p.FriendlyId.ToLower().Contains(searchTerm)) ||
                                (isGuidSearch && p.Id == guidSearch)))
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
            }

            if (canViewCases)
            {
                // Do not bypass query filters: the Case filter is the shared, hierarchy-aware
                // disease-access boundary used by pages, APIs and the case access service.
                var cases = await _context.Cases
                    .Include(c => c.Patient)
                    .Include(c => c.Disease)
                    .Where(c => !c.IsDeleted &&
                               c.Patient != null &&
                               (c.Patient.GivenName.ToLower().Contains(searchTerm) ||
                                c.Patient.FamilyName.ToLower().Contains(searchTerm) ||
                                (c.Patient.City != null && c.Patient.City.ToLower().Contains(searchTerm)) ||
                                (c.FriendlyId != null && c.FriendlyId.ToLower().Contains(searchTerm)) ||
                                (isGuidSearch && c.Id == guidSearch)))
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
            }

            if (canViewCases)
            {
                // Contacts are case records and therefore share Case.View and the same
                // hierarchy-aware global filter as surveillance cases.
                var contacts = await _context.Cases
                    .Include(c => c.Patient)
                    .Where(c => !c.IsDeleted &&
                               c.Type == CaseType.Contact &&
                               c.Patient != null &&
                               (c.Patient.GivenName.ToLower().Contains(searchTerm) ||
                                c.Patient.FamilyName.ToLower().Contains(searchTerm) ||
                                (c.Patient.AddressLine != null && c.Patient.AddressLine.ToLower().Contains(searchTerm)) ||
                                (c.Patient.City != null && c.Patient.City.ToLower().Contains(searchTerm)) ||
                                (c.FriendlyId != null && c.FriendlyId.ToLower().Contains(searchTerm)) ||
                                (isGuidSearch && c.Id == guidSearch)))
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
            }

            if (canViewOutbreaks)
            {
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
            }

            if (canViewEvents)
            {
                var events = await _context.Events
                    .Include(e => e.Location)
                    .Include(e => e.EventType)
                    .Where(e => e.IsActive &&
                               (e.Name.ToLower().Contains(searchTerm) ||
                                (e.Description != null && e.Description.ToLower().Contains(searchTerm)) ||
                                (e.Location != null && e.Location.Name.ToLower().Contains(searchTerm)) ||
                                (e.Location != null && e.Location.Address != null && e.Location.Address.ToLower().Contains(searchTerm)) ||
                                (isGuidSearch && e.Id == guidSearch)))
                    .Take(5)
                    .Select(e => new QuickSearchResult
                    {
                        Type = "Event",
                        Id = e.Id.ToString(),
                        Title = e.Name,
                        Subtitle = e.EventType != null ? e.EventType.Name : (e.Location != null ? e.Location.Name : ""),
                        Icon = "calendar-event",
                        Url = $"/Events/Details?id={e.Id}"
                    })
                    .ToListAsync();
                results.AddRange(events);
            }

            if (canViewLocations)
            {
                var locations = await _context.Locations
                    .Include(l => l.LocationType)
                    .Include(l => l.Organization)
                    .Where(l => l.IsActive &&
                               (l.Name.ToLower().Contains(searchTerm) ||
                                (l.Address != null && l.Address.ToLower().Contains(searchTerm)) ||
                                (l.Notes != null && l.Notes.ToLower().Contains(searchTerm)) ||
                                (l.Organization != null && l.Organization.Name.ToLower().Contains(searchTerm)) ||
                                (isGuidSearch && l.Id == guidSearch)))
                    .Take(5)
                    .Select(l => new QuickSearchResult
                    {
                        Type = "Location",
                        Id = l.Id.ToString(),
                        Title = l.Name,
                        Subtitle = l.LocationType != null ? l.LocationType.Name : (l.Address ?? ""),
                        Icon = "geo-alt",
                        Url = $"/Locations/Details?id={l.Id}"
                    })
                    .ToListAsync();
                results.AddRange(locations);
            }

            if (canViewOrganizations)
            {
                var organizations = await _context.Organizations
                    .Include(o => o.OrganizationType)
                    .Where(o => o.IsActive &&
                               (o.Name.ToLower().Contains(searchTerm) ||
                                (o.ContactPerson != null && o.ContactPerson.ToLower().Contains(searchTerm)) ||
                                (o.Address != null && o.Address.ToLower().Contains(searchTerm)) ||
                                (o.FriendlyId != null && o.FriendlyId.ToLower().Contains(searchTerm)) ||
                                (isGuidSearch && o.Id == guidSearch)))
                    .Take(5)
                    .Select(o => new QuickSearchResult
                    {
                        Type = "Organization",
                        Id = o.Id.ToString(),
                        Title = o.Name,
                        Subtitle = o.OrganizationType != null ? o.OrganizationType.Name : (o.Address ?? ""),
                        Icon = "building",
                        Url = $"/Organizations/Details?id={o.Id}"
                    })
                    .ToListAsync();
                results.AddRange(organizations);
            }

            return new JsonResult(new { results = results.Take(20) });
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
