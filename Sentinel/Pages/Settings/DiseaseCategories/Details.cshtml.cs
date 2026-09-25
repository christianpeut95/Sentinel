using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Sentinel.Data;
using Sentinel.Models.Lookups;

namespace Sentinel.Pages.Settings.DiseaseCategories;

[Authorize(Policy = "Permission.Settings.ManageSystemLookups")]
public class DetailsModel : PageModel
{
    private readonly ApplicationDbContext _context;

    public DetailsModel(ApplicationDbContext context)
    {
        _context = context;
    }

    public DiseaseCategory Category { get; private set; } = null!;
    public int DiseaseCount { get; private set; }

    public async Task<IActionResult> OnGetAsync(Guid id)
    {
        Category = await _context.DiseaseCategories
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == id);

        if (Category == null)
        {
            return NotFound();
        }

        DiseaseCount = await _context.Diseases.CountAsync(d => d.DiseaseCategoryId == id);
        return Page();
    }
}
