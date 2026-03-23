namespace SUI.Find.Application.Models;

public record SubmitJobResultsRequest
{
    public required string JobId { get; init; }

    public required string LeaseId { get; init; }

    public required string ResultType { get; init; }

    public required List<JobResultRecord> Records { get; init; }
}
