namespace SUI.Find.Application.Models;

public abstract record SearchResult
{
    public sealed record Success(SearchResultsDto Result) : SearchResult;

    public sealed record NotFound : SearchResult;

    public sealed record Unauthorized : SearchResult;

    public sealed record Failed : SearchResult;
}
