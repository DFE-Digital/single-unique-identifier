namespace SUI.Find.Application.Models;

public record CustodianRecord
{
    public string RecordId { get; init; } = string.Empty;
    public string PersonId { get; init; } = string.Empty;
    public string RecordType { get; init; } = string.Empty;
    public int Version { get; init; }
    public string SchemaUri { get; init; } = string.Empty;
    public ContactDetails? ContactDetails { get; init; }

    public RecordLink? RecordLink { get; init; }
    public System.Text.Json.JsonElement? Payload { get; init; }
}
