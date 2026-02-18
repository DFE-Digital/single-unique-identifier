namespace SUI.StubCustodians.Application.Models;

public sealed class CustodianRecord
{
    public string RecordId { get; set; } = default!;
    public string PersonId { get; set; } = default!;
    public string EncryptedPersonId { get; set; } = default!;
    public string RecordType { get; set; } = default!;
    public int Version { get; set; }
    public string SchemaUri { get; set; } = default!;
    public ContactDetails ContactDetails { get; set; } = default!;
    public RecordLink RecordLink { get; set; } = default!;
    public System.Text.Json.JsonElement Payload { get; set; }
}

public class ContactDetails
{
    public string Name { get; set; } = default!;
    public string Description { get; set; } = default!;
    public string Telephone { get; set; } = default!;
    public string Email { get; set; } = default!;
    public string Address { get; set; } = default!;
}

public class RecordLink
{
    public string Url { get; set; } = default!;
    public string Title { get; set; } = default!;
}
