using SUI.UIHarness.Web.Models.Find;

namespace SUI.UIHarness.Web.Models;

public record SearchResultsDto(
    string WorkItemId,
    FindSearchStatus Status,
    FindSearchResultItem[] Items,
    int CompletenessPercentage
);
