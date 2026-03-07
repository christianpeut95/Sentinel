using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Sentinel.Data;
using Sentinel.Models;
using Sentinel.Services;

namespace Sentinel.Pages.Cases
{
    [Authorize]
    public class AuditHistoryModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly IAuditService _auditService;

        public AuditHistoryModel(ApplicationDbContext context, IAuditService auditService)
        {
            _context = context;
            _auditService = auditService;
        }

        public Case Case { get; set; } = default!;
        public Guid CaseId { get; set; }
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

            var caseEntity = await _context.Cases
                .Include(c => c.Patient)
                .Include(c => c.Disease)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (caseEntity == null)
            {
                return NotFound();
            }

            Case = caseEntity;
            CaseId = id.Value;

            var allLogs = await _auditService.GetAuditLogsAsync("Case", CaseId.ToString());

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
