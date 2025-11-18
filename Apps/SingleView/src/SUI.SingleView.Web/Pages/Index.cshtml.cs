using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace SUI.SingleView.Web.Pages;

public class Index : PageModel
{
    private readonly ILogger<Index> _logger;

    public Index(ILogger<Index> logger)
    {
        _logger = logger;
    }

    public PageResult OnGet()
    {
        return Page();
    }
}
