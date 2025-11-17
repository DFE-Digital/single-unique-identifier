using SUI.SingleView.Domain.Models;

namespace SUI.SingleView.Application.Services;

public interface ISearchService
{
    List<SearchResult> Search(string nhsNumber);

    List<SearchResult> Search(
        string? firstName,
        string? lastName,
        DateTime? dateOfBirth,
        string? houseNumberOrName,
        string? postcode
    );
}
