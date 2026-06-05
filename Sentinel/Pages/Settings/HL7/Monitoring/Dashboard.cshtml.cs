using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Sentinel.Data;
using Sentinel.Models;
using Sentinel.Services.HL7;

namespace Sentinel.Pages.Settings.HL7.Monitoring
{
    [Authorize(Policy = "Permission.Settings.ManageSystemLookups")]
    public class DashboardModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly IHL7FileMonitorService _fileMonitor;

        public DashboardModel(ApplicationDbContext context, IHL7FileMonitorService fileMonitor)
        {
            _context = context;
            _fileMonitor = fileMonitor;
        }

        public MonitoringStatus MonitoringStatus { get; set; } = null!;
        public List<HL7Configuration> ActiveConfigurations { get; set; } = new();
        public List<HL7Message> RecentMessages { get; set; } = new();

        public async Task OnGetAsync()
        {
            // Get monitoring status
            MonitoringStatus = _fileMonitor.GetMonitoringStatus();

            // Get active configurations
            ActiveConfigurations = await _context.HL7Configurations
                .Where(c => c.IsActive)
                .OrderBy(c => c.Priority)
                .ToListAsync();

            // Get recent messages (last 50)
            RecentMessages = await _context.HL7Messages
                .Include(m => m.Case)
                .Include(m => m.Patient)
                .OrderByDescending(m => m.ReceivedAt)
                .Take(50)
                .ToListAsync();
        }
    }
}
