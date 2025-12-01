using SUI.Find.Application.Enums;

namespace SUI.Find.Application.Models;

public abstract record SearchJobResult
{
    public sealed record Success(SearchJobDto Job) : SearchJobResult;

    public sealed record NotFound : SearchJobResult;

    public sealed record Unauthorized : SearchJobResult;

    public sealed record Failed : SearchJobResult;
}

public record SearchJobDto
{
    public string JobId { get; init; } = null!;
    public string Suid { get; init; } = string.Empty;
    public SearchStatus Status { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
    public DateTimeOffset LastUpdatedAt { get; init; }
}
