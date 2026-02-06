using Microsoft.Extensions.Logging;
using OneOf;
using OneOf.Types;
using SUI.Find.Application.Dtos;
using SUI.Find.Application.Interfaces;
using SUI.Find.Application.Models;

namespace SUI.Find.Application.Services;

public class MaskUrlService(
    ILogger<MaskUrlService> logger,
    IFetchUrlStorageService fetchUrlStorageService
) : IMaskUrlService
{
    public async Task<IReadOnlyList<CustodianSearchResultItem>> CreateAsync(
        List<CustodianSearchResultItem> items,
        QueryProviderInput input,
        CancellationToken ct
    )
    {
        var masked = new List<CustodianSearchResultItem>();
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
                logger.LogWarning(ex, "Failed to mask URL: {Error}", ex.Message);
                // Do nothing else, we don't want to reveal original URL if masking fails
            }
        }

        return masked;
    }

    public async Task<OneOf<ResolvedFetchMapping, NotFound, Unauthorized, Error>> ResolveAsync(
        string requestingOrg,
        string fetchId,
        CancellationToken ct
    )
    {
        try
        {
            var res = await fetchUrlStorageService.GetAsync(
                requestingOrg: requestingOrg,
                fetchId: fetchId,
                ct: ct
            );

            return res;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to get masked URL");
            return new Error();
        }
    }
}
