using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Sentinel.Data;
using Sentinel.Models;
using Sentinel.Services;

namespace Sentinel.Pages.Outbreaks;

[Authorize(Policy = "Permission.Outbreak.View")]
public class LineListModel : PageModel
{
    private readonly ApplicationDbContext _context;
    private readonly IAuthorizationService _authorizationService;
    private readonly IOutbreakAccessService _outbreakAccessService;
    
    public LineListModel(
        ApplicationDbContext context,
        IAuthorizationService authorizationService,
        IOutbreakAccessService outbreakAccessService)
    {
        _context = context;
        _authorizationService = authorizationService;
        _outbreakAccessService = outbreakAccessService;
    }
    
    public Outbreak Outbreak { get; set; } = null!;
    public bool CanExport { get; private set; }
    
    public async Task<IActionResult> OnGetAsync(int id)
    {
        if (!await _outbreakAccessService.CanAccessOutbreakAsync(id))
        {
            return NotFound();
        }

        var outbreak = await _context.Outbreaks
            .FirstOrDefaultAsync(o => o.Id == id);
        
        if (outbreak == null)
        {
            return NotFound();
        }
        
        Outbreak = outbreak;
        CanExport = (await _authorizationService.AuthorizeAsync(User, "Permission.Outbreak.Export")).Succeeded;
        return Page();
    }
}
