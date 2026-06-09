using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using SUI.AuthEmulator.Configurations;

namespace SUI.AuthEmulator.Controllers;

[ApiController]
[Route("api/v1/.well-known/openid-configuration")]
public class OIDCDiscoveryController : ControllerBase
{
    private readonly AuthSettings _authSettings;

    public OIDCDiscoveryController(IOptions<AuthSettings> authSettings)
    {
        _authSettings = authSettings.Value;
    }

    [HttpGet]
    public IActionResult GetDiscoveryDocument()
    {
        var baseUrl = _authSettings.BaseUrl.TrimEnd('/');

        var discoveryDocument = new
        {
            issuer = _authSettings.Issuer,
            token_endpoint = $"{baseUrl}/api/v1/auth/token",
            jwks_uri = $"{baseUrl}/api/v1/jwks",
            id_token_signing_alg_values_supported = new[] { "RS256" },
            grant_types_supported = new[] { "client_credentials" },
            response_types_supported = new[] { "token" },
            subject_types_supported = new[] { "public" },
            authorization_endpoint = $"{baseUrl}/dummy",
        };

        return Ok(discoveryDocument);
    }
}
