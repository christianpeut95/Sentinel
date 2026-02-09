using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Surveillance_MVP.Data;
using Surveillance_MVP.Models;

namespace Surveillance_MVP.Pages.Outbreaks;

[Authorize(Policy = "Permission.Outbreak.View")]
public class LineListModel : PageModel
{
    private readonly ApplicationDbContext _context;
    
    public LineListModel(ApplicationDbContext context)
    {
        _context = context;
    }
    
    public Outbreak Outbreak { get; set; } = null!;
    
    public async Task<IActionResult> OnGetAsync(int id)
    {
        var outbreak = await _context.Outbreaks
            .FirstOrDefaultAsync(o => o.Id == id);
        
        if (outbreak == null)
        {
            return NotFound();
        }
        
        Outbreak = outbreak;
        return Page();
    }
}
