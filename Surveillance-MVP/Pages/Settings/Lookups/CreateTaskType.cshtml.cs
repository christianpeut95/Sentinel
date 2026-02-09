using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Surveillance_MVP.Data;
using Surveillance_MVP.Models.Lookups;
using System.ComponentModel.DataAnnotations;

namespace Surveillance_MVP.Pages.Settings.Lookups
{
    [Authorize(Policy = "Permission.Settings.Create")]
    public class CreateTaskTypeModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public CreateTaskTypeModel(ApplicationDbContext context)
        {
            _context = context;
        }

        [BindProperty]
        public InputModel Input { get; set; } = new();

        public class InputModel
        {
            [Required]
            [StringLength(100)]
            public string Name { get; set; } = string.Empty;

            [StringLength(20)]
            public string? Code { get; set; }

            [StringLength(500)]
            public string? Description { get; set; }

            [StringLength(50)]
            [Display(Name = "Icon Class")]
            public string IconClass { get; set; } = "bi-clipboard-check";

            [StringLength(50)]
            [Display(Name = "Color Class")]
            public string ColorClass { get; set; } = "bg-info";

            [Display(Name = "Display Order")]
            public int DisplayOrder { get; set; }

            [Display(Name = "Is Active")]
            public bool IsActive { get; set; } = true;
        }

        public void OnGet()
        {
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            var taskType = new TaskType
            {
                Id = Guid.NewGuid(),
                Name = Input.Name,
                Code = Input.Code,
                Description = Input.Description,
                IconClass = Input.IconClass,
                ColorClass = Input.ColorClass,
                DisplayOrder = Input.DisplayOrder,
                IsActive = Input.IsActive
            };

            _context.TaskTypes.Add(taskType);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Task type '{taskType.Name}' has been created successfully.";
            return RedirectToPage("./TaskTypes");
        }
    }
}
