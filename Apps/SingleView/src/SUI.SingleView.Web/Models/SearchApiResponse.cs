using SUI.SingleView.Domain.Models;

namespace SUI.SingleView.Web.Models;

public class SearchApiResponse
{
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public IList<SearchResult>? Results { get; set; }
}
