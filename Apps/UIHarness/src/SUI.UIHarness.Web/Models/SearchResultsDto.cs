using SUI.UIHarness.Web.Models.Find;

namespace SUI.UIHarness.Web.Models;

public record SearchResultsDto(
    string JobId,
    FindSearchStatus Status,
    FindSearchResultItem[] Items,
    int CompletenessPercentage
);
