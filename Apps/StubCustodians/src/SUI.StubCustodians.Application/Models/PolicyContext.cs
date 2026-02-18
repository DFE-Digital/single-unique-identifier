using System.Diagnostics.CodeAnalysis;

namespace SUI.StubCustodians.Application.Models;

[ExcludeFromCodeCoverage(Justification = "Mock service")]
public sealed record PolicyContext(
    string Subject,
    string Organisation,
    IReadOnlyList<string> Roles,
    string Purpose,
    IReadOnlyList<string> DsaIds
);
