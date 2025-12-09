namespace SUI.StubCustodians.Application.Models;

public class RecordEnvelope<T>
    where T : class
{
    public required Uri SchemaUri { get; init; }

    public required T Payload { get; init; }
}
