using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Sentinel.Data;
using Sentinel.Models;
using Sentinel.Models.CaseDefinitions;

namespace Sentinel.Pages.Cases
{
    [Authorize(Policy = "Permission.Case.View")]
    public class ClassificationHistoryModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public ClassificationHistoryModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public Case Case { get; set; } = default!;
        public List<CaseClassificationHistory> ClassificationHistory { get; set; } = new();
        public Dictionary<string, string> UserDisplayNames { get; set; } = new();

        public async Task<IActionResult> OnGetAsync(Guid? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var caseEntity = await _context.Cases
                .Include(c => c.Disease)
                .Include(c => c.Patient)
                .Include(c => c.ConfirmationStatus)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (caseEntity == null)
            {
                return NotFound();
            }

            Case = caseEntity;

            // Load classification history with related data
            ClassificationHistory = await _context.CaseClassificationHistory
                .Include(h => h.CaseDefinition)
                .Include(h => h.FromConfirmationStatus)
                .Include(h => h.ToConfirmationStatus)
                .Where(h => h.CaseId == id)
                .OrderByDescending(h => h.EvaluationDate)
                .ToListAsync();

            // Load user display names for all users who classified cases
            var userIds = ClassificationHistory
                .Where(h => !string.IsNullOrEmpty(h.ClassifiedByUserId))
                .Select(h => h.ClassifiedByUserId!)
                .Distinct()
                .ToList();

            if (userIds.Any())
            {
                var users = await _context.Users
                    .Where(u => userIds.Contains(u.Id))
                    .ToListAsync();

                UserDisplayNames = users.ToDictionary(
                    u => u.Id,
                    u => GetUserDisplayName(u)
                );
            }

            return Page();
        }

        private string GetUserDisplayName(ApplicationUser user)
        {
            // Priority: FirstName LastName > Email > UserName
            if (!string.IsNullOrWhiteSpace(user.FirstName) || !string.IsNullOrWhiteSpace(user.LastName))
            {
                return $"{user.FirstName} {user.LastName}".Trim();
            }

            if (!string.IsNullOrWhiteSpace(user.Email))
            {
                return user.Email;
            }

            return user.UserName ?? "Unknown User";
        }
    }
}
