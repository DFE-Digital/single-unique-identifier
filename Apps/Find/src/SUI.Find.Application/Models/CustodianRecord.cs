namespace SUI.Find.Application.Models;

public record CustodianRecord
{
    public string RecordId { get; set; } = string.Empty;
    public string PersonId { get; set; } = string.Empty;
    public string RecordType { get; set; } = string.Empty;
    public string DataType { get; set; } = string.Empty;
    public string SchemaUri { get; set; } = string.Empty;
    public System.Text.Json.JsonElement? Payload { get; set; }
}