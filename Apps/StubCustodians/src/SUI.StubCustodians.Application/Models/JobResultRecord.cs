namespace SUI.StubCustodians.Application.Models;

public record JobResultRecord
{
    public required string RecordType { get; init; }

    public required string RecordUrl { get; init; }

    public string? SystemId { get; init; }

    public string? RecordId { get; init; }
}
