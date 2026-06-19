namespace SUI.Find.FindApi.Models;

public sealed record AuthContext(
    string ClientId,
    string OrganisationId,
    IReadOnlyList<string> Scopes,
    bool IsEnabled
);
