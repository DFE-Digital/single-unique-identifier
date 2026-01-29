using System.Diagnostics.CodeAnalysis;

namespace SUI.StubCustodians.Application.Models;

[ExcludeFromCodeCoverage(Justification = "Mock service")]
public sealed class AuthClient
{
    public string ClientId { get; set; } = string.Empty;
    public string ClientSecret { get; set; } = string.Empty;
    public bool Enabled { get; set; } = true;
    public List<string>? AllowedScopes { get; set; }
}
