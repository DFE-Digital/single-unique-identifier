using Microsoft.Extensions.Caching.Memory;
using SUI.Transfer.Application.Models.Custodians;

namespace SUI.Transfer.Application.Services;

public class Repository(IMemoryCache memoryCache) : IRepository
{
    public void AddOrUpdate(ConsolidatedData consolidatedData)
    {
        if (
            memoryCache.TryGetValue(consolidatedData.Sui, out ConsolidatedData? cacheValue)
            && !consolidatedData.Equals(cacheValue)
        )
            return;

        // Cache is always updated on change of data or after expiry, regardless of errors
        cacheValue = consolidatedData;

        var cacheEntryOptions = new MemoryCacheEntryOptions().SetSlidingExpiration(
            TimeSpan.FromMinutes(5)
        );

        memoryCache.Set(consolidatedData.Sui, cacheValue, cacheEntryOptions);
    }
}
