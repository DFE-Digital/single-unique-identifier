using Microsoft.AspNetCore.Authentication;

namespace SUI.Transfer.Infrastructure.Authentication;

public class AuthenticationOptions : AuthenticationSchemeOptions
{
    public const string DefaultScheme = "ApiKeyAuthenticationScheme";
    public string ApiKeyHeader { get; set; } = "X-Api-Key";
}
