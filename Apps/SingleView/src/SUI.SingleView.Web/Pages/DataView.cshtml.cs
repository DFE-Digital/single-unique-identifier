using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SUI.SingleView.Application.Models;
using SUI.SingleView.Application.Services;

namespace SUI.SingleView.Web.Pages;

public class DataView(ILogger<DataView> logger, IRecordService recordService) : PageModel
{
    private readonly ILogger<DataView> _logger = logger;

    public PersonModel? PersonModel { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? PersonId { get; set; }

    public async Task<PageResult> OnGetAsync(bool prefetch = false)
    {
        if (prefetch && !string.IsNullOrWhiteSpace(PersonId))
        {
            PersonModel = await recordService.GetRecordAsync(PersonId);
        }

        return Page();
    }
}
