using Microsoft.AspNetCore.Mvc.RazorPages;
using Sentinel.Data;
using Sentinel.Tools;

namespace Sentinel.Pages.Tools;

public class GenerateFieldInventoryModel : PageModel
{
    private readonly ApplicationDbContext _context;

    public GenerateFieldInventoryModel(ApplicationDbContext context)
    {
        _context = context;
    }

    public string? Inventory { get; set; }

    public void OnGet()
    {
        // Initial page load
    }

    public void OnPost()
    {
        var generator = new FieldInventoryGenerator(_context);
        Inventory = generator.GenerateInventory();
    }
}
