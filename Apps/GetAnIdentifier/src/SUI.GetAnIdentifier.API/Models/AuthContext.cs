namespace SUI.GetAnIdentifier.API.Models;

public sealed record AuthContext(
    string ClientId,
    string OrganisationId,
    IReadOnlyList<string> Scopes
);
