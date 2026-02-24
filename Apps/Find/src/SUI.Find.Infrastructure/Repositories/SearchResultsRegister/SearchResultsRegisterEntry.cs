namespace SUI.Find.Infrastructure.Repositories.SearchResultsRegister;

public class SearchResultsRegisterEntry
{
    public required string CustodianId { get; init; }
    public required string SystemId { get; init; }
    public required string RecordType { get; init; }
    public required string RecordUrl { get; init; }
    public DateTime SubmittedAtUtc { get; init; }
    public required string JobId { get; init; }
}
