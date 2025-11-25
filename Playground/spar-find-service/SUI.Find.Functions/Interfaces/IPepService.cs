using Models;

namespace Interfaces;

public interface IPepService
{
    IReadOnlyList<SearchResultItem> Filter(PolicyContext policy, IReadOnlyList<SearchResultItem> raw);
}
