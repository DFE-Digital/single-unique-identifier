using SUi.Find.Application.Constants;

namespace SUi.Find.Application.Models;

public class SearchSpecification : PersonSpecification
{
    private string _searchStrategy = SharedConstants.SearchStrategy.Strategies.Strategy1;

    public string SearchStrategy
    {
        get => _searchStrategy;
        init => _searchStrategy =
            string.IsNullOrEmpty(value) ? SharedConstants.SearchStrategy.Strategies.Strategy1 : value;
    }

    public int? StrategyVersion { get; set; }
}