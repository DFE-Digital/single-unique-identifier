using System.Diagnostics.CodeAnalysis;
using SUI.SingleView.Application.Models;
using SUI.SingleView.Domain;
using SUI.SingleView.Domain.Models;

namespace SUI.SingleView.Application.Services;

public class SearchService : ISearchService
{
    public List<SearchResult> Search(string nhsNumber) =>
        Search(new SearchQuery(nhsNumber, null, null, null, null, null));

    public List<SearchResult> Search(
        string? firstName,
        string? lastName,
        DateTime? dateOfBirth,
        string? houseNumberOrName,
        string? postcode
    ) =>
        Search(
            new SearchQuery(null, firstName, lastName, dateOfBirth, houseNumberOrName, postcode)
        );

    private static string NormalizePostcode(string? postcode) =>
        string.IsNullOrWhiteSpace(postcode)
            ? string.Empty
            : postcode.Replace(" ", "", StringComparison.Ordinal).ToUpperInvariant();

    private List<SearchResult> Search(SearchQuery query)
    {
        // TODO: Replace this with an actual call to the matcher API
        return HardcodedSearch(query);
    }

    /// <summary>
    /// Temporary search implementation until searching API implemented.
    /// </summary>
    [ExcludeFromCodeCoverage]
    private static List<SearchResult> HardcodedSearch(SearchQuery query)
    {
        IEnumerable<SearchResult> results = HardCodedSearchResults.All;

        // 1. If an NHS number is provided, treat as the primary key and only match on that
        if (!string.IsNullOrWhiteSpace(query.NhsNumber))
        {
            var digits = new string(query.NhsNumber.Where(char.IsDigit).ToArray());

            results = results.Where(r =>
                string.Equals(r.NhsNumber.Value, digits, StringComparison.Ordinal)
            );

            return results.ToList();
        }

        // 2. Name-based search
        if (!string.IsNullOrWhiteSpace(query.FirstName))
        {
            var first = query.FirstName.Trim();
            results = results.Where(r =>
            {
                var parts = r.Name.Split(
                    ' ',
                    StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries
                );
                var candidateFirst = parts.FirstOrDefault() ?? string.Empty;
                return candidateFirst.StartsWith(first, StringComparison.OrdinalIgnoreCase);
            });
        }

        if (!string.IsNullOrWhiteSpace(query.LastName))
        {
            var last = query.LastName.Trim();
            results = results.Where(r =>
            {
                var parts = r.Name.Split(
                    ' ',
                    StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries
                );
                var candidateLast = parts.LastOrDefault() ?? string.Empty;
                return candidateLast.StartsWith(last, StringComparison.OrdinalIgnoreCase);
            });
        }

        // 3. DOB (exact date)
        if (query.DateOfBirth is not null)
        {
            var dob = query.DateOfBirth.Value.Date;
            results = results.Where(r => r.DateOfBirth.Date == dob);
        }

        // 4. House number / name (prefix match on AddressLine1)
        if (!string.IsNullOrWhiteSpace(query.HouseNumberOrName))
        {
            var house = query.HouseNumberOrName.Trim();
            results = results.Where(r =>
                r.Address.AddressLine1.StartsWith(house, StringComparison.OrdinalIgnoreCase)
            );
        }

        // 5. Postcode (normalised)
        if (!string.IsNullOrWhiteSpace(query.Postcode))
        {
            var qPostcode = NormalizePostcode(query.Postcode);
            results = results.Where(r => NormalizePostcode(r.Address.Postcode) == qPostcode);
        }

        return results.ToList();
    }
}
