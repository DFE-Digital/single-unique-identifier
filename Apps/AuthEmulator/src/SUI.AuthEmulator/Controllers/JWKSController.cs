using Microsoft.AspNetCore.Mvc;
using SUI.AuthEmulator.Services;

namespace SUI.AuthEmulator.Controllers;

[ApiController]
[Route("api/v1/jwks")]
public class JWKSController : ControllerBase
{
    private readonly IJwksKeyProvider _jwksKeyProvider;

    public JWKSController(IJwksKeyProvider jwksKeyProvider)
    {
        _jwksKeyProvider = jwksKeyProvider;
    }

    [HttpGet]
    public IActionResult GetJwks()
    {
        var rsaKeys = _jwksKeyProvider.GetKeys();

        // Returns public key information in the prescribed JWKS specification format
        var jwksDocument = new
        {
            keys = rsaKeys.Select(rsaKey => new
            {
                kty = "RSA",
                use = "sig",
                alg = "RS256",
                kid = rsaKey.Key.KeyId,
                n = rsaKey.Modulus, // Public Modulus
                e = rsaKey.Exponent, // Public Exponent
            }),
        };

        return Ok(jwksDocument);
    }
}
