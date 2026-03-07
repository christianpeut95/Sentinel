using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Sentinel.Models;
using Sentinel.Services;

namespace Sentinel.Pages.Patients
{
    public class AuditHistoryModel : PageModel
    {
        private readonly IAuditService _auditService;

        public AuditHistoryModel(IAuditService auditService)
        {
            _auditService = auditService;
        }

        public Guid PatientId { get; set; }
        public List<AuditLog> AuditLogs { get; set; } = new List<AuditLog>();
        public int TotalChanges { get; set; }
        public int ViewCount { get; set; }
        public int DataChangeCount { get; set; }

        [BindProperty(SupportsGet = true)]
        public bool ShowViews { get; set; } = true;

        public async Task<IActionResult> OnGetAsync(Guid? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            PatientId = id.Value;
            var allLogs = await _auditService.GetAuditLogsAsync("Patient", PatientId.ToString());

            ViewCount = allLogs.Count(l => l.Action == "Viewed");
            DataChangeCount = allLogs.Count(l => l.Action != "Viewed");

            if (!ShowViews)
            {
                AuditLogs = allLogs.Where(l => l.Action != "Viewed").ToList();
            }
            else
            {
                AuditLogs = allLogs;
            }

            TotalChanges = AuditLogs.Count;

            return Page();
        }
    }
}
