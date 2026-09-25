using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Sentinel.Data;
using Sentinel.Models.Lookups;

namespace Sentinel.Pages.Settings.DiseaseCategories;

[Authorize(Policy = "Permission.Settings.ManageSystemLookups")]
public class DeleteModel : PageModel
{
    private readonly ApplicationDbContext _context;

    public DeleteModel(ApplicationDbContext context)
    {
        _context = context;
    }

    public DiseaseCategory Category { get; private set; } = null!;
    public int DiseaseCount { get; private set; }

    public async Task<IActionResult> OnGetAsync(Guid id)
    {
        return await LoadAsync(id) ? Page() : NotFound();
    }

    public async Task<IActionResult> OnPostAsync(Guid id)
    {
        var category = await _context.DiseaseCategories.FirstOrDefaultAsync(c => c.Id == id);
        if (category == null)
        {
            return NotFound();
        }

        var diseaseCount = await _context.Diseases.CountAsync(d => d.DiseaseCategoryId == id);
        if (diseaseCount > 0)
        {
            TempData["ErrorMessage"] = $"'{category.Name}' cannot be deleted because it is assigned to {diseaseCount} disease(s). Reassign those diseases or mark the category inactive instead.";
            return RedirectToPage("./Details", new { id });
        }

        _context.DiseaseCategories.Remove(category);
        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = $"Category '{category.Name}' has been deleted.";
        return RedirectToPage("./Index");
    }

    private async Task<bool> LoadAsync(Guid id)
    {
        Category = await _context.DiseaseCategories
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == id);

        if (Category == null)
        {
            return false;
        }

        DiseaseCount = await _context.Diseases.CountAsync(d => d.DiseaseCategoryId == id);
        return true;
    }
}
