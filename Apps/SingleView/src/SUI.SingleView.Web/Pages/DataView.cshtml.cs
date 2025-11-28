using Microsoft.AspNetCore.Mvc.RazorPages;
using SUI.SingleView.Application.Models;
using SUI.SingleView.Application.Services;

namespace SUI.SingleView.Web.Pages;

public class DataView : PageModel
{
    private readonly ILogger<DataView> _logger;
    private readonly IRecordService _recordService;

    public PersonModel PersonModel { get; set; } = null!;

    public DataView(ILogger<DataView> logger, IRecordService recordService)
    {
        _logger = logger;
        _recordService = recordService;
    }

    public async Task OnGetAsync()
    {
        PersonModel = await _recordService.GetRecord("1234567890");
    }
}
