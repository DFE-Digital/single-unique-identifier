namespace SUI.UIHarness.Web.Models.Find;

public record FindCustodianRecord
{
    public string RecordId { get; init; } = string.Empty;
    public string PersonId { get; init; } = string.Empty;
    public string RecordType { get; init; } = string.Empty;
    public int Version { get; init; }
    public string SchemaUri { get; init; } = string.Empty;
    public List<ContactDetail>? ContactDetails { get; init; }
    public List<RecordLink>? RecordLinks { get; init; }
    public System.Text.Json.JsonElement? Payload { get; init; }
}
