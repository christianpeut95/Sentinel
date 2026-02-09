using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Surveillance_MVP.Services;
using Surveillance_MVP.Models;

namespace Surveillance_MVP.Pages.Outbreaks;

[Authorize(Policy = "Permission.Outbreak.View")]
public class IndexModel : PageModel
{
    private readonly IOutbreakService _outbreakService;

    public IndexModel(IOutbreakService outbreakService)
    {
        _outbreakService = outbreakService;
    }

    public List<Outbreak> Outbreaks { get; set; } = new();
    public bool ShowInactive { get; set; }

    public async Task OnGetAsync(bool showInactive = false)
    {
        ShowInactive = showInactive;
        Outbreaks = await _outbreakService.GetAllAsync(showInactive);
    }
}
