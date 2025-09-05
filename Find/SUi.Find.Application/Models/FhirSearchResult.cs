namespace SUi.Find.Application.Models;

public class FhirSearchResult
{
    public required ResultType Type { get; init; } = ResultType.Unmatched;

    public decimal? Score { get; init; }

    public string? NhsNumber { get; init; }

    public string? ErrorMessage { get; init; }

    public enum ResultType
    {
        Matched,
        Unmatched,
        MultiMatched,
        Error
    }

    public static FhirSearchResult Match(string nhsNumber, decimal? score) => new()
    {
        Type = ResultType.Matched,
        NhsNumber = nhsNumber,
        Score = score,
    };

    public static FhirSearchResult Unmatched() => new() { Type = ResultType.Unmatched };

    public static FhirSearchResult MultiMatched() => new() { Type = ResultType.MultiMatched };

    public static FhirSearchResult Error(string errorMessage) => new() { Type = ResultType.Error, ErrorMessage = errorMessage };
}