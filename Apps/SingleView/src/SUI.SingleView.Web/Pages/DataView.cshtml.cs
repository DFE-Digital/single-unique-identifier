using Microsoft.AspNetCore.Mvc.RazorPages;
using SUI.SingleView.Application.Models;

namespace SUI.SingleView.Web.Pages;

public class DataView : PageModel
{
    private readonly ILogger<DataView> _logger;

    public PersonModel PersonModel { get; set; }

    public DataView(ILogger<DataView> logger)
    {
        _logger = logger;
    }

    public void OnGet()
    {
        PersonModel = new PersonModel { Name = "Test Person", NhsNumber = "1234567890" };
    }
}
