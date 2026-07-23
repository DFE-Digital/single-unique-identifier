namespace SUI.GetAnIdentifier.Function.Models;

public sealed record AuthContext(
    string ClientId,
    string OrganisationId,
    IReadOnlyList<string> Scopes
);
