using Microsoft.Extensions.Logging;
using SUI.Find.Application.Dtos;
using SUI.Find.Application.Interfaces;
using SUI.Find.Application.Models;

namespace SUI.Find.Application.Services;

public class MaskUrlService(
    ILogger<MaskUrlService> logger,
    IFetchUrlStorageService fetchUrlStorageService
)
{
    public async Task<IReadOnlyList<SearchResultItem>> CreateMaskedFetchUrlsAsync(
        List<SearchResultItem> items,
        QueryProviderInput input,
        CancellationToken ct
    )
    {
        var masked = new List<SearchResultItem>(items.Count);
        foreach (var item in items)
        {
            try
            {
                var ttl = TimeSpan.FromMinutes(10);
                var fetchId = Guid.NewGuid().ToString("N");
                var expiresAt = DateTimeOffset.UtcNow.Add(ttl);
                var dto = new AddFetchUrlRequest
                {
                    FetchId = fetchId,
                    JobId = input.JobId,
                    TargetUrl = item.RecordUrl,
                    TargetOrg = input.Provider.OrgId,
                    RequestingOrg = input.RequestingOrg,
                    RecordType = item.RecordType,
                    Ttl = ttl,
                };
                await fetchUrlStorageService.AddAsync(dto, ct);

                var maskedUrl = $"/v1/records/{fetchId}";
                var mapping = new MaskedUrl(fetchId, maskedUrl, expiresAt);
                var rewritten = item with { RecordUrl = mapping.Url };
                masked.Add(rewritten);
            }
            catch (Exception ex)
            {
                logger.LogWarning("Failed to mask URL: {Error}", ex.Message);
                // Do nothing else, we don't want to reveal original URL if masking fails
            }
        }

        return masked;
    }
}
