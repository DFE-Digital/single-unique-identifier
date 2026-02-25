using SUI.UIHarness.Web.Models.Find;

namespace SUI.UIHarness.Web.Models;

public record LocalCustodianRecord<T>()
{
    public string RecordId { get; init; } = string.Empty;
    public string PersonId { get; init; } = string.Empty;
    public string RecordType { get; init; } = string.Empty;
    public int Version { get; init; }
    public string SchemaUri { get; init; } = string.Empty;
    public ContactDetails? ContactDetails { get; init; }

    public RecordLink? RecordLink { get; init; }
    public T? Payload { get; init; }
}
