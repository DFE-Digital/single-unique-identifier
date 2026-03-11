namespace SUI.Find.Application.Models;

public record JobResultRecord
{
    public required string SystemId { get; init; }

    //public required string RecordId { get; init; }

    public required string RecordType { get; init; }

    public required string RecordUrl { get; init; }
}
