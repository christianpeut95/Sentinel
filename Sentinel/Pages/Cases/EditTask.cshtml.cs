using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Sentinel.Data;
using Sentinel.Models;
using System.Security.Claims;

namespace Sentinel.Pages.Cases
{
    [Authorize(Policy = "Permission.Case.Edit")]
    public class EditTaskModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public EditTaskModel(ApplicationDbContext context)
        {
            _context = context;
        }

        [BindProperty]
        public CaseTask Task { get; set; } = default!;

        public Guid CaseId { get; set; }

        public SelectList StatusList { get; set; } = default!;
        public SelectList PriorityList { get; set; } = default!;

        public async Task<IActionResult> OnGetAsync(Guid? id, Guid? caseId)
        {
            if (id == null || caseId == null)
            {
                return NotFound();
            }

            CaseId = caseId.Value;

            Task = await _context.CaseTasks
                .Include(t => t.TaskTemplate)
                .Include(t => t.TaskType)
                .Include(t => t.AssignedToUser)
                .FirstOrDefaultAsync(t => t.Id == id && t.CaseId == caseId);

            if (Task == null)
            {
                return NotFound();
            }

            LoadDropdowns();
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                LoadDropdowns();
                return Page();
            }

            var taskToUpdate = await _context.CaseTasks.FindAsync(Task.Id);

            if (taskToUpdate == null)
            {
                return NotFound();
            }

            // Update fields
            taskToUpdate.Status = Task.Status;
            taskToUpdate.Priority = Task.Priority;
            taskToUpdate.DueDate = Task.DueDate;
            taskToUpdate.AssignedToUserId = Task.AssignedToUserId;
            taskToUpdate.ModifiedAt = DateTime.UtcNow;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!TaskExists(Task.Id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            // Close window and refresh parent
            return Content("<script>window.opener.location.reload(); window.close();</script>", "text/html");
        }

        private bool TaskExists(Guid id)
        {
            return _context.CaseTasks.Any(e => e.Id == id);
        }

        private void LoadDropdowns()
        {
            StatusList = new SelectList(new[]
            {
                new { Value = 0, Text = "Pending" },
                new { Value = 1, Text = "In Progress" },
                new { Value = 2, Text = "Completed" },
                new { Value = 3, Text = "Cancelled" },
                new { Value = 4, Text = "Overdue" },
                new { Value = 5, Text = "Waiting for Patient" }
            }, "Value", "Text");

            PriorityList = new SelectList(new[]
            {
                new { Value = 0, Text = "Low" },
                new { Value = 1, Text = "Medium" },
                new { Value = 2, Text = "High" },
                new { Value = 3, Text = "Urgent" }
            }, "Value", "Text");
        }
    }
}
