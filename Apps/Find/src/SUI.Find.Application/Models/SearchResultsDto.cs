using SUI.Find.Application.Enums;

namespace SUI.Find.Application.Models;

public class SearchResultsDto
{
    public string JobId { get; private init; } = string.Empty;
    public string Suid { get; private init; } = string.Empty;
    public SearchStatus Status { get; private init; }
    public SearchResultItem[] Items { get; private init; } = [];
    public SearchResultsStatus ResultsStatus { get; private init; }
    public string? ErrorMessage { get; private init; }

    public static SearchResultsDto Success(
        string jobId,
        string suid,
        SearchStatus status,
        SearchResultItem[] items
    ) =>
        new()
        {
            JobId = jobId,
            Suid = suid,
            Status = status,
            Items = items,
            ResultsStatus = SearchResultsStatus.Success,
        };

    public static SearchResultsDto Unauthorized(string jobId, string? errorMessage = null) =>
        new()
        {
            JobId = jobId,
            Suid = string.Empty,
            Status = SearchStatus.Failed,
            Items = [],
            ResultsStatus = SearchResultsStatus.Unauthorized,
            ErrorMessage = errorMessage,
        };

    public static SearchResultsDto NotFound(string jobId, string? errorMessage = null) =>
        new()
        {
            JobId = jobId,
            Suid = string.Empty,
            Status = SearchStatus.Failed,
            Items = [],
            ResultsStatus = SearchResultsStatus.NotFound,
            ErrorMessage = errorMessage,
        };

    public static SearchResultsDto Error(string jobId, string? errorMessage = null) =>
        new()
        {
            JobId = jobId,
            Suid = string.Empty,
            Status = SearchStatus.Failed,
            Items = [],
            ResultsStatus = SearchResultsStatus.Error,
            ErrorMessage = errorMessage,
        };
}
