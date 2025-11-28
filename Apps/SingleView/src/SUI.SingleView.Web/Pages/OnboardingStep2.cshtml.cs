using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace SUI.SingleView.Web.Pages;

public class OnboardingStep2(ILogger<OnboardingStep2> logger) : PageModel
{
    private readonly ILogger<OnboardingStep2> _logger = logger;

    public PageResult OnGet() => Page();
}
