using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Sentinel.Data;
using Sentinel.Models;
using Sentinel.Services.HL7;

namespace Sentinel.Pages.Settings.HL7.Configurations
{
    [Authorize(Policy = "Permission.Settings.ManageSystemLookups")]
    public class IndexModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly IHL7FileMonitorService _fileMonitor;

        public IndexModel(ApplicationDbContext context, IHL7FileMonitorService fileMonitor)
        {
            _context = context;
            _fileMonitor = fileMonitor;
        }

        public List<HL7Configuration> Configurations { get; set; } = new();
        public MonitoringStatus? MonitoringStatus { get; set; }

        public async Task<IActionResult> OnGetAsync()
        {
            Configurations = await _context.HL7Configurations
                .OrderBy(c => c.Priority)
                .ThenBy(c => c.ConfigurationName)
                .ToListAsync();

            // Get real-time monitoring status
            MonitoringStatus = _fileMonitor.GetMonitoringStatus();

            return Page();
        }

        public async Task<IActionResult> OnPostAsync(Guid id)
        {
            var config = await _context.HL7Configurations.FindAsync(id);
            if (config == null)
            {
                return NotFound();
            }

            _context.HL7Configurations.Remove(config);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Configuration '{config.ConfigurationName}' has been deleted.";
            return RedirectToPage();
        }
    }
}
