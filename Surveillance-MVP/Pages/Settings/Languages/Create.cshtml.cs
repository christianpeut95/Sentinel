using System;
using Microsoft.AspNetCore.Authorization;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Surveillance_MVP.Data;
using Surveillance_MVP.Models.Lookups;

namespace Surveillance_MVP.Pages.Settings.Languages
{
    [Authorize(Policy = "Permission.Settings.ManageSystemLookups")]
    public class CreateModel : PageModel
    {
        private readonly Surveillance_MVP.Data.ApplicationDbContext _context;

        public CreateModel(Surveillance_MVP.Data.ApplicationDbContext context)
        {
            _context = context;
        }

        public IActionResult OnGet()
        {
            return Page();
        }

        [BindProperty]
        public Language Language { get; set; } = default!;

        // For more information, see https://aka.ms/RazorPagesCRUD.
        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            _context.Languages.Add(Language);
            await _context.SaveChangesAsync();

            return RedirectToPage("./Index");
        }
    }
}
