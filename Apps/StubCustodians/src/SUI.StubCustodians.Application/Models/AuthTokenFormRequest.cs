using System.Diagnostics.CodeAnalysis;

namespace SUI.StubCustodians.Application.Models;

[ExcludeFromCodeCoverage(Justification = "Mock service")]
public sealed class AuthTokenFormRequest
{
    public string client_id { get; set; } = string.Empty;
    public string client_secret { get; set; } = string.Empty;
    public string scope { get; set; } = string.Empty;
    public string grant_type { get; set; } = "client_credentials";
}
