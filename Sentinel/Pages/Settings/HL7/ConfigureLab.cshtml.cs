using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Sentinel.Data;
using Sentinel.Models;

namespace Sentinel.Pages.Settings.HL7
{
    [Authorize(Policy = "Permission.HL7.Configure")]
    public class ConfigureLabPageModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<ConfigureLabPageModel> _logger;

        public ConfigureLabPageModel(ApplicationDbContext context, ILogger<ConfigureLabPageModel> logger)
        {
            _context = context;
            _logger = logger;
        }

        [BindProperty(SupportsGet = true)]
        public Guid ConfigId { get; set; }

        public HL7Configuration? Configuration { get; set; }
        public string ConfigurationName { get; set; } = "Lab Configuration";

        public async Task<IActionResult> OnGetAsync()
        {
            if (ConfigId == Guid.Empty)
            {
                _logger.LogWarning("ConfigureLab accessed without a valid ConfigId");
                return RedirectToPage("/Settings/HL7/FieldMappings/SelectLab");
            }

            Configuration = await _context.HL7Configurations
                .Where(c => c.Id == ConfigId)
                .FirstOrDefaultAsync();

            if (Configuration == null)
            {
                _logger.LogWarning("HL7 Configuration {ConfigId} not found", ConfigId);
                return Page();
            }

            ConfigurationName = Configuration.ConfigurationName ?? "Lab Configuration";

            return Page();
        }
    }
}
