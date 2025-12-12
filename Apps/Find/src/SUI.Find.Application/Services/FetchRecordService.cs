using System.Text.Json;
using Microsoft.Extensions.Logging;
using OneOf;
using OneOf.Types;
using SUI.Find.Application.Interfaces;
using SUI.Find.Application.Models;

namespace SUI.Find.Application.Services;

public class FetchRecordService(
    ILogger<FetchRecordService> logger,
    IMaskUrlService maskUrlService,
    ICustodianService custodianService,
    IProviderHttpClient providerClient,
    IOutboundAuthService outboundAuthService
) : IFetchRecordService
{
    public async Task<OneOf<CustodianRecord, NotFound, Unauthorized, Error>> FetchRecordAsync(
        string fetchId,
        string requestingOrgId,
        CancellationToken cancellationToken
    )
    {
        var resolvedMapping = await maskUrlService.ResolveAsync(
            requestingOrgId,
            fetchId,
            cancellationToken
        );

        return resolvedMapping.Value switch
        {
            ResolvedFetchMapping successDto => await GetCustodianDataAsync(
                successDto,
                cancellationToken
            ),
            NotFound notFound => notFound,
            Unauthorized unauthorized => unauthorized,
            Error error => error,
            _ => new Error(),
        };
    }

    private async Task<OneOf<CustodianRecord, NotFound, Unauthorized, Error>> GetCustodianDataAsync(
        ResolvedFetchMapping resolvedMapping,
        CancellationToken cancellationToken
    )
    {
        var provider = await custodianService.GetCustodianAsync(resolvedMapping.TargetOrgId);

        if (!provider.Success || provider.Value is null)
        {
            logger.LogError(
                "Failed to retrieve custodian organisation for Org ID {OrgId}",
                resolvedMapping.TargetOrgId
            );
            return new Error();
        }

        var tokenResult = await outboundAuthService.GetAccessTokenAsync(
            provider.Value,
            CancellationToken.None
        );

        if (!tokenResult.Success || string.IsNullOrWhiteSpace(tokenResult.Value))
        {
            return new Error();
        }

        var response = await providerClient.GetAsync(
            resolvedMapping.TargetUrl,
            tokenResult.Value,
            cancellationToken
        );

        if (!response.Success)
        {
            return new Error();
        }

        if (string.IsNullOrWhiteSpace(response.Value))
        {
            logger.LogInformation("Fetch Record returned empty response");
            return new Error();
        }

        var recordContent = JsonSerializer.Deserialize<CustodianRecord>(
            response.Value,
            JsonSerializerOptions.Web
        );

        return recordContent is not null ? recordContent : new Error();
    }
}
