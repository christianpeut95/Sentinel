using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Surveillance_MVP.Data;

namespace Surveillance_MVP.Pages
{
    public class IndexModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public IndexModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task OnGetAsync()
        {
            // Total patients
            ViewData["TotalPatients"] = await _context.Patients.CountAsync();

            // New patients this month
            var startOfMonth = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1);
            ViewData["NewPatientsThisMonth"] = await _context.Patients
                .Where(p => p.CreatedAt >= startOfMonth)
                .CountAsync();

            // Recent views (last 24 hours)
            var yesterday = DateTime.UtcNow.AddDays(-1);
            ViewData["RecentViews"] = await _context.AuditLogs
                .Where(a => a.Action == "Viewed" && a.ChangedAt >= yesterday)
                .CountAsync();

            // Recent edits (last 24 hours)
            ViewData["RecentEdits"] = await _context.AuditLogs
                .Where(a => a.Action == "Modified" && a.ChangedAt >= yesterday)
                .CountAsync();
        }
    }
}
