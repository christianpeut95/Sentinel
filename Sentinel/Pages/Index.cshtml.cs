using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using Sentinel.Data;
using Sentinel.Models;

namespace Sentinel.Pages
{
    public class IndexModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public IndexModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public List<RecentlyViewedItem> RecentlyViewed { get; set; } = new();
        public List<Outbreak> MyOutbreaks { get; set; } = new();
        public List<CaseTask> MyTasks { get; set; } = new();

        public class RecentlyViewedItem
        {
            public string EntityType { get; set; } = string.Empty;
            public string EntityId { get; set; } = string.Empty;
            public string DisplayName { get; set; } = string.Empty;
            public string SubText { get; set; } = string.Empty;
            public string Url { get; set; } = string.Empty;
            public DateTime ViewedAt { get; set; }
        }

        public async Task OnGetAsync()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null) return;

            // Recently viewed: get the latest distinct views across Cases, Contacts, and Patients
            var recentLogs = await _context.AuditLogs
                .Where(a => a.ChangedByUserId == userId && a.Action == "Viewed"
                            && (a.EntityType == "Case" || a.EntityType == "Patient"))
                .OrderByDescending(a => a.ChangedAt)
                .Take(100)
                .ToListAsync();

            var distinctViews = recentLogs
                .GroupBy(a => new { a.EntityType, a.EntityId })
                .Select(g => g.First())
                .Take(10)
                .ToList();

            var caseIds = distinctViews
                .Where(v => v.EntityType == "Case" && Guid.TryParse(v.EntityId, out _))
                .Select(v => Guid.Parse(v.EntityId))
                .ToList();

            var patientIds = distinctViews
                .Where(v => v.EntityType == "Patient" && Guid.TryParse(v.EntityId, out _))
                .Select(v => Guid.Parse(v.EntityId))
                .ToList();

            var cases = caseIds.Any()
                ? await _context.Cases
                    .Include(c => c.Patient)
                    .Include(c => c.Disease)
                    .Where(c => caseIds.Contains(c.Id) && !c.IsDeleted)
                    .ToListAsync()
                : new List<Case>();

            var patients = patientIds.Any()
                ? await _context.Patients
                    .Where(p => patientIds.Contains(p.Id))
                    .ToListAsync()
                : new List<Patient>();

            RecentlyViewed = distinctViews
                .Select(v =>
                {
                    if (v.EntityType == "Case" && Guid.TryParse(v.EntityId, out var caseId))
                    {
                        var c = cases.FirstOrDefault(x => x.Id == caseId);
                        if (c == null) return null;
                        var isContact = c.Type == CaseType.Contact;
                        return new RecentlyViewedItem
                        {
                            EntityType = isContact ? "Contact" : "Case",
                            EntityId = v.EntityId,
                            DisplayName = $"{c.Patient?.GivenName} {c.Patient?.FamilyName}".Trim(),
                            SubText = isContact ? "Contact" : (c.FriendlyId ?? "Case"),
                            Url = isContact ? $"/Contacts/Details/{c.Id}" : $"/Cases/Details/{c.Id}",
                            ViewedAt = v.ChangedAt
                        };
                    }
                    else if (v.EntityType == "Patient" && Guid.TryParse(v.EntityId, out var patId))
                    {
                        var p = patients.FirstOrDefault(x => x.Id == patId);
                        if (p == null) return null;
                        return new RecentlyViewedItem
                        {
                            EntityType = "Patient",
                            EntityId = v.EntityId,
                            DisplayName = $"{p.GivenName} {p.FamilyName}".Trim(),
                            SubText = "Patient",
                            Url = $"/Patients/Details/{p.Id}",
                            ViewedAt = v.ChangedAt
                        };
                    }
                    return null;
                })
                .Where(v => v != null)
                .Select(v => v!)
                .ToList();

            // My Outbreaks: active outbreaks the current user is a team member of
            MyOutbreaks = await _context.OutbreakTeamMembers
                .Include(tm => tm.Outbreak)
                    .ThenInclude(o => o.PrimaryDisease)
                .Where(tm => tm.UserId == userId && tm.IsActive
                             && (tm.Outbreak.Status == OutbreakStatus.Active || tm.Outbreak.Status == OutbreakStatus.Monitoring))
                .OrderByDescending(tm => tm.AssignedDate)
                .Select(tm => tm.Outbreak)
                .ToListAsync();

            // My Assigned Tasks
            MyTasks = await _context.CaseTasks
                .Include(t => t.Case)
                    .ThenInclude(c => c!.Patient)
                .Include(t => t.TaskTemplate)
                .Where(t => t.AssignedToUserId == userId 
                            && (t.Status == CaseTaskStatus.Pending || t.Status == CaseTaskStatus.InProgress))
                .OrderBy(t => t.Priority)
                .ThenBy(t => t.DueDate)
                .Take(10)
                .ToListAsync();
        }
    }
}
