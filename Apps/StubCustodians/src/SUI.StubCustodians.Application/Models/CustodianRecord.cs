namespace SUI.StubCustodians.Application.Models;

public sealed class CustodianRecord
{
    public string RecordId { get; set; } = string.Empty;
    public string PersonId { get; set; } = string.Empty;
    public string EncryptedPersonId { get; set; } = string.Empty;
    public string RecordType { get; set; } = string.Empty;
    public int Version { get; set; }
    public string SchemaUri { get; set; } = string.Empty;
    public ContactDetails? ContactDetails { get; set; }
    public RecordLink? RecordLink { get; set; }
    public System.Text.Json.JsonElement? Payload { get; set; }
}
