using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;

namespace UIHarness.Controllers;

public sealed class AccountController : Controller
{
    [HttpGet("/account/login")]
    public IActionResult Login()
    {
        return View();
    }

    [HttpPost("/account/login")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login([FromForm] string org)
    {
        if (string.IsNullOrWhiteSpace(org))
        {
            ModelState.AddModelError("org", "Select an organisation.");
            return View();
        }

        var claims = new List<Claim>
        {
            new("org", org),
            new(ClaimTypes.Name, org)
        };

        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        var principal = new ClaimsPrincipal(identity);

        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            principal,
            new AuthenticationProperties { IsPersistent = true });

        return RedirectToAction("Index", "Home");
    }

    [HttpPost("/account/logout")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return RedirectToAction(nameof(Login));
    }
}
