using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SUI.SingleView.Web.Extensions;

namespace SUI.SingleView.Web.Pages;

[ValidateAntiForgeryToken]
public class SignIn(ILogger<SignIn> logger, IValidator<SignIn> validator) : PageModel
{
    [BindProperty]
    public string? Username { get; set; } = string.Empty;

    [BindProperty]
    public string? Password { get; set; } = string.Empty;

    private readonly ILogger<SignIn> _logger = logger;

    public PageResult OnGet()
    {
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        var result = await validator.ValidateAsync(this);
        if (!result.IsValid)
        {
            result.AddToModelState(ModelState);
            return Page();
        }

        // TODO: Do real authentication handling
        if (Username != "user" || Password != "pass")
        {
            ModelState.AddModelError("Username", "Your username or password is incorrect");
            ModelState.AddModelError("Password", "Your username or password is incorrect");
            return Page();
        }

        return RedirectToPage("/Search"); // TODO: SUI-1045 Add search page
    }
}
