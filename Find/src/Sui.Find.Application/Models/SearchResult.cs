namespace SUi.Find.Application.Models;

public class SearchResult
{
    public required ResultType Type { get; init; } = ResultType.Unmatched;

    public decimal? Score { get; init; }

    public string? NhsNumber { get; init; }

    public enum ResultType
    {
        Matched,
        Unmatched,
        MultiMatched,
        Error
    }

    public static SearchResult Match(string nhsNumber, decimal? score) => new()
    {
        Type = ResultType.Matched,
        NhsNumber = nhsNumber,
        Score = score,
    };

    public static SearchResult Unmatched() => new() { Type = ResultType.Unmatched };

    public static SearchResult MultiMatched() => new() { Type = ResultType.MultiMatched };

}