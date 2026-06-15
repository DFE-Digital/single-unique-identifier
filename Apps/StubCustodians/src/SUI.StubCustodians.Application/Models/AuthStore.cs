using System.Diagnostics.CodeAnalysis;

namespace SUI.StubCustodians.Application.Models;

[ExcludeFromCodeCoverage(Justification = "Mock service")]
public sealed class AuthStore
{
    public List<AuthClient>? Clients { get; set; }
}
