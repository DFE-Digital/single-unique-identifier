namespace SUI.StubCustodians.Application.Models;

public class RecordEnvelope<T>
    where T : class
{
    public required string RecordId { get; init; }

    public required string PersonId { get; init; }

    public required Uri SchemaUri { get; init; }

    public required string RecordType { get; init; }

    public required int Version { get; init; }

    public required ContactDetails ContactDetails { get; init; }

    public required RecordLink RecordLink { get; init; }

    public required T Payload { get; init; }
}
