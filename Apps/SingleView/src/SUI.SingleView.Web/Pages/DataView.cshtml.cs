using Microsoft.AspNetCore.Mvc.RazorPages;
using SUI.SingleView.Application.Models;
using SUI.SingleView.Application.Services;

namespace SUI.SingleView.Web.Pages;

public class DataView : PageModel
{
    private readonly ILogger<DataView> _logger;
    private readonly IRecordService _recordService;

    public PersonModel PersonModel { get; set; }

    public DataView(ILogger<DataView> logger, IRecordService recordService)
    {
        _logger = logger;
        _recordService = recordService;
    }

    public void OnGet()
    {
        PersonModel = _recordService.GetRecord("1234567890");
    }
}
