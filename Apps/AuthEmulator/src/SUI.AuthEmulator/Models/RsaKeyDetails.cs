using Microsoft.IdentityModel.Tokens;

namespace SUI.AuthEmulator.Models;

public record RsaKeyDetails(
    RsaSecurityKey Key,
    SigningCredentials SigningCredentials,
    string Modulus,
    string Exponent
);
