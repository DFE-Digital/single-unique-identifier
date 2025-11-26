using Microsoft.AspNetCore.Mvc.RazorPages;

namespace SUI.SingleView.Web.Pages;

public class OnboardingStep1(ILogger<OnboardingStep1> logger) : PageModel
{
    private readonly ILogger<OnboardingStep1> _logger = logger;

    public PageResult OnGet() => Page();
}
