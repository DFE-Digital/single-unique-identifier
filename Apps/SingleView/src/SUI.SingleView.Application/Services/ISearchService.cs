using SUI.SingleView.Domain.Models;

namespace SUI.SingleView.Application.Services;

public interface ISearchService
{
    Task<IList<SearchResult>> SearchAsync(string nhsNumber);

    Task<IList<SearchResult>> SearchAsync(
        string? firstName,
        string? lastName,
        DateTime? dateOfBirth,
        string? houseNumberOrName,
        string? postcode
    );
}
