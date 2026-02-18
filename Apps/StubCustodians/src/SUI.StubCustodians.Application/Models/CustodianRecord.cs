namespace SUI.StubCustodians.Application.Models;

public sealed class CustodianRecord
{
    public string RecordId { get; set; } = default!;
    public string PersonId { get; set; } = default!;
    public string EncryptedPersonId { get; set; } = default!;
    public string RecordType { get; set; } = default!;
    public int Version { get; set; }
    public string SchemaUri { get; set; } = default!;
    public string SystemId { get; set; } = default!;
    public System.Text.Json.JsonElement Payload { get; set; }
}
