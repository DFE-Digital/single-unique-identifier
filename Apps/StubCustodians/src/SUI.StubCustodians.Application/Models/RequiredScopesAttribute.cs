using System.Diagnostics.CodeAnalysis;

namespace SUI.StubCustodians.Application.Models;

[ExcludeFromCodeCoverage(Justification = "Mock service")]
[AttributeUsage(AttributeTargets.Method, Inherited = false)]
public sealed class RequiredScopesAttribute : Attribute
{
    public RequiredScopesAttribute(params string[]? scopes)
    {
        Scopes = scopes ?? [];
    }

    public IReadOnlyList<string> Scopes { get; }
}
