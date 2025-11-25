using Interfaces;
using Models;

public sealed class PepService : IPepService
{
    public IReadOnlyList<SearchResultItem> Filter(PolicyContext policy, IReadOnlyList<SearchResultItem> raw)
    {
        return raw;
    }
}
