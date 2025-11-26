using System.Diagnostics.CodeAnalysis;

namespace SUI.Find.FindApi.Models;

[ExcludeFromCodeCoverage(Justification = "Will be tested with integration tests.")]
public sealed record AuthContext(string ClientId, IReadOnlyList<string> Scopes);
