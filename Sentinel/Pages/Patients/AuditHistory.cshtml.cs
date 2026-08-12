using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Sentinel.Data;
using Sentinel.Models;
using Sentinel.Services;

namespace Sentinel.Pages.Patients
{
    [Authorize(Policy = "Permission.Patient.View")]
    [Authorize(Policy = "Permission.Audit.View")]
    public class AuditHistoryModel : PageModel
    {
        private readonly IAuditService _auditService;
        private readonly ApplicationDbContext _context;

        public AuditHistoryModel(IAuditService auditService, ApplicationDbContext context)
        {
            _auditService = auditService;
            _context = context;
        }

        public Guid PatientId { get; set; }
        public string PatientFriendlyId { get; set; } = string.Empty;
        public List<AuditLog> AuditLogs { get; set; } = new List<AuditLog>();
        public int TotalChanges { get; set; }
        public int ViewCount { get; set; }
        public int DataChangeCount { get; set; }

        [BindProperty(SupportsGet = true)]
        public bool ShowViews { get; set; } = false;

        public async Task<IActionResult> OnGetAsync(Guid? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            PatientId = id.Value;

            // Get patient friendly ID
            var patient = await _context.Patients
                .Where(p => p.Id == PatientId)
                .Select(p => p.FriendlyId)
                .FirstOrDefaultAsync();

            if (patient == null)
            {
                return NotFound();
            }

            PatientFriendlyId = patient ?? PatientId.ToString();

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

        public string FormatFieldName(string fieldName)
        {
            // Remove "Id" suffix and add spaces before capitals
            var formatted = fieldName.EndsWith("Id") ? fieldName.Substring(0, fieldName.Length - 2) : fieldName;
            return System.Text.RegularExpressions.Regex.Replace(formatted, "([a-z])([A-Z])", "$1 $2");
        }

        public string FormatFieldValue(string fieldName, string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return string.Empty;

            // If it's a lookup field (ends with Id), try to resolve it
            if (fieldName.EndsWith("Id") && int.TryParse(value, out int id) && id > 0)
            {
                // Try to resolve the lookup value synchronously (this is called during rendering)
                var lookupValue = ResolveLookupValue(fieldName, id);
                if (!string.IsNullOrEmpty(lookupValue))
                    return lookupValue;
            }

            // Handle boolean values
            if (value.Equals("True", StringComparison.OrdinalIgnoreCase))
                return "Yes";
            if (value.Equals("False", StringComparison.OrdinalIgnoreCase))
                return "No";

            // Handle dates
            if (DateTime.TryParse(value, out DateTime dateValue))
            {
                return dateValue.ToString("dd MMM yyyy");
            }

            return value;
        }

        private string? ResolveLookupValue(string fieldName, int id)
        {
            try
            {
                return fieldName switch
                {
                    "SexAtBirthId" => _context.SexAtBirths.Where(x => x.Id == id).Select(x => x.Name).FirstOrDefault(),
                    "GenderId" => _context.Genders.Where(x => x.Id == id).Select(x => x.Name).FirstOrDefault(),
                    "AtsiStatusId" => _context.AtsiStatuses.Where(x => x.Id == id).Select(x => x.Name).FirstOrDefault(),
                    "OccupationId" => _context.Occupations.Where(x => x.Id == id).Select(x => x.Name).FirstOrDefault(),
                    "CountryOfBirthId" => _context.Countries.Where(x => x.Id == id).Select(x => x.Name).FirstOrDefault(),
                    "LanguageSpokenAtHomeId" => _context.Languages.Where(x => x.Id == id).Select(x => x.Name).FirstOrDefault(),
                    "AncestryId" => _context.Ancestries.Where(x => x.Id == id).Select(x => x.Name).FirstOrDefault(),
                    "StateId" => _context.States.Where(x => x.Id == id).Select(x => x.Name).FirstOrDefault(),
                    "Jurisdiction1Id" => _context.Jurisdictions.Where(x => x.Id == id).Select(x => x.Name).FirstOrDefault(),
                    "Jurisdiction2Id" => _context.Jurisdictions.Where(x => x.Id == id).Select(x => x.Name).FirstOrDefault(),
                    "Jurisdiction3Id" => _context.Jurisdictions.Where(x => x.Id == id).Select(x => x.Name).FirstOrDefault(),
                    "Jurisdiction4Id" => _context.Jurisdictions.Where(x => x.Id == id).Select(x => x.Name).FirstOrDefault(),
                    "Jurisdiction5Id" => _context.Jurisdictions.Where(x => x.Id == id).Select(x => x.Name).FirstOrDefault(),
                    _ => null
                };
            }
            catch
            {
                return null;
            }
        }
    }
}
