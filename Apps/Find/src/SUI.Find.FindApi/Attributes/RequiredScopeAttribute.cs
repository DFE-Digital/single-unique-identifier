using System.Diagnostics.CodeAnalysis;

namespace SUI.Find.FindApi.Attributes;

[ExcludeFromCodeCoverage(
    Justification = "Attribute class does not contain any logic to be tested."
)]
[AttributeUsage(AttributeTargets.Method, Inherited = false)]
public sealed class RequiredScopesAttribute : Attribute
{
    public RequiredScopesAttribute(params string[]? scopes)
    {
        Scopes = scopes ?? [];
    }

    public IReadOnlyList<string> Scopes { get; }
}
