namespace SUI.Find.Application.Models;

public abstract record SearchJobResult
{
    public sealed record Success(SearchJobDto Job) : SearchJobResult;

    public sealed record NotFound : SearchJobResult;

    public sealed record Unauthorized : SearchJobResult;

    public sealed record Failed : SearchJobResult;
}
