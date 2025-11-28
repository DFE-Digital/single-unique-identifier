namespace SUI.Find.Application.Models;

public sealed record PolicyContext(string ClientId, IReadOnlyList<string> Scopes);
