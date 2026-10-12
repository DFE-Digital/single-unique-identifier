namespace SUI.GetAnIdentifier.API.Models;

public sealed record AuthContext(string ClientId, IReadOnlyList<string> Scopes);
