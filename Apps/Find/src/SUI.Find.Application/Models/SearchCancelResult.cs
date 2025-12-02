using SUI.Find.Application.Dtos;

namespace SUI.Find.Application.Models;

public abstract record SearchCancelResult
{
    public sealed record Success(SearchJobDto Result) : SearchCancelResult;

    public sealed record NotFound : SearchCancelResult;

    public sealed record Unauthorized : SearchCancelResult;

    public sealed record Error : SearchCancelResult;
}
