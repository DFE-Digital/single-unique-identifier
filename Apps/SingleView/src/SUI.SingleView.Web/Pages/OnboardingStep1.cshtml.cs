using Microsoft.AspNetCore.Mvc.RazorPages;

namespace SUI.SingleView.Web.Pages;

public class OnboardingStep1 : PageModel
{
    private readonly ILogger<OnboardingStep1> _logger;

    public string NextStep { get; set; } = "OnboardingStep2";

    public OnboardingStep1(ILogger<OnboardingStep1> logger)
    {
        _logger = logger;
    }

    public void OnGet()
    {
        if (Request.Cookies.ContainsKey("AcceptedTerms"))
        {
            NextStep = "SignIn";
        }
    }
}
