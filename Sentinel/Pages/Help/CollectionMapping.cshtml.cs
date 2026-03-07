using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Sentinel.Pages.Help;

[Authorize]
public class CollectionMappingModel : PageModel
{
    public void OnGet()
    {
    }
}
