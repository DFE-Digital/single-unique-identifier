using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace SUI.Transfer.Infrastructure.Authentication;

public class ApiKeyAuthenticationHandler(
    IOptionsMonitor<AuthenticationOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder,
    ISystemClock clock
) : AuthenticationHandler<AuthenticationOptions>(options, logger, encoder, clock)
{
    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Request.Headers.TryGetValue(Options.ApiKeyHeader, out var requestKey))
        {
            return await Task.FromResult(
                AuthenticateResult.Fail($"Missing header: {Options.ApiKeyHeader}")
            );
        }

        var configuration =
            Request.HttpContext.RequestServices.GetRequiredService<IConfiguration>();
        var apiKey = configuration["Authentication:ApiKey"];

        if (!requestKey.Equals(apiKey))
        {
            return await Task.FromResult(
                AuthenticateResult.Fail($"Invalid token: {Options.ApiKeyHeader}")
            );
        }

        var claims = new List<Claim> { new("Username", "dev") };

        var claimsIdentity = new ClaimsIdentity(claims, Scheme.Name);
        var claimsPrincipal = new ClaimsPrincipal(claimsIdentity);

        return await Task.FromResult(
            AuthenticateResult.Success(new AuthenticationTicket(claimsPrincipal, Scheme.Name))
        );
    }
}
