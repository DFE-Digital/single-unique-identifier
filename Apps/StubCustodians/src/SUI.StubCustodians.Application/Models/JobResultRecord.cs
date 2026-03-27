namespace SUI.StubCustodians.Application.Models;

public record JobResultRecord
{
    public required string RecordType { get; init; }

    public required string RecordUrl { get; init; }

    public string SystemId
    {
        get;
        init => field = string.IsNullOrWhiteSpace(value) ? "DEFAULT" : value;
    } = "DEFAULT";

    public string? RecordId { get; init; }
}
