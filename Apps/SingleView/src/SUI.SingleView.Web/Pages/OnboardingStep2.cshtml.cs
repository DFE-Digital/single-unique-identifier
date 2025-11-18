using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace SUI.SingleView.Web.Pages;

public class OnboardingStep2 : PageModel
{
    private readonly ILogger<OnboardingStep2> _logger;

    public bool ShowError { get; set; }

    [BindProperty]
    public List<string>? SelectedOptions { get; set; }

    public OnboardingStep2(ILogger<OnboardingStep2> logger)
    {
        _logger = logger;
    }

    public void OnGet() { }

    public IActionResult OnPost()
    {
        if (SelectedOptions == null || !SelectedOptions.Contains("terms"))
        {
            ShowError = true;
            return Page();
        }

        var cookieOptions = new CookieOptions
        {
            Expires = DateTime.Now.AddDays(7),
            HttpOnly = true,
            Secure = true,
        };

        if (SelectedOptions.Contains("terms"))
        {
            Response.Cookies.Append("AcceptedTerms", "True", cookieOptions);
        }

        if (SelectedOptions.Contains("analytics"))
        {
            Response.Cookies.Append("AcceptedAnalytics", "True", cookieOptions);
        }

        return RedirectToPage("./Signin");
    }
}
