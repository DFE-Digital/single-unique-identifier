using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using SUI.UIHarness.Web.Components;
using SUI.UIHarness.Web.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents().AddInteractiveServerComponents();

builder.Services.AddCascadingAuthenticationState();
builder
    .Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/login";
    });
builder.Services.AddAuthorization();

builder.Services.AddHttpClient(
    nameof(FindService),
    client => client.BaseAddress = new Uri(builder.Configuration["BaseUrl"] + "api/")
);
builder.Services.AddScoped<IFindService, FindService>();
builder.Services.AddScoped<IFindApiAuthClientProvider, FindApiAuthClientProvider>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();
app.UseAntiforgery();

app.MapStaticAssets();

app.MapPost(
        "/api/auth/login",
        async (
            [FromForm] string custodianName,
            [FromForm] string password,
            [FromForm] string architecture,
            [FromServices] IConfiguration configuration,
            HttpContext httpContext
        ) =>
        {
            var expectedPassword =
                configuration.GetValue<string>("UI_TEST_HARNESS_PASSWORD") ?? "local-dev-only";
            if (password != expectedPassword)
            {
                return Results.Redirect(
                    $"/login?error=invalid_password&custodianName={Uri.EscapeDataString(custodianName)}&architecture={Uri.EscapeDataString(architecture)}"
                );
            }

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, custodianName),
                new Claim("Architecture", architecture),
                new Claim("Password", password),
            };

            var identity = new ClaimsIdentity(
                claims,
                CookieAuthenticationDefaults.AuthenticationScheme
            );
            var principal = new ClaimsPrincipal(identity);

            await httpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                principal
            );

            return Results.Redirect("/");
        }
    )
    .DisableAntiforgery();

app.MapPost(
        "/api/auth/logout",
        async (HttpContext httpContext) =>
        {
            await httpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return Results.Redirect("/login");
        }
    )
    .DisableAntiforgery();

app.MapRazorComponents<App>().AddInteractiveServerRenderMode();

app.Run();
