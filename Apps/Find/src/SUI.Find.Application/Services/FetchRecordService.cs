using System.Text.Json;
using Microsoft.Extensions.Logging;
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
    public async Task<Domain.Models.Result<CustodianRecord>> FetchRecordAsync(
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
            NotFound _ => Domain.Models.Result<CustodianRecord>.Fail("NotFound"),
            Unauthorized _ => Domain.Models.Result<CustodianRecord>.Fail("Unauthorized"),
            _ => Domain.Models.Result<CustodianRecord>.Fail("Failed to fetch mapping."),
        };
    }

    private async Task<Domain.Models.Result<CustodianRecord>> GetCustodianDataAsync(
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
            return Domain.Models.Result<CustodianRecord>.Fail(
                provider.Error ?? "Failed to retrieve custodian organisation."
            );
        }

        var tokenResult = await outboundAuthService.GetAccessTokenAsync(
            provider.Value,
            CancellationToken.None
        );

        if (!tokenResult.Success || string.IsNullOrWhiteSpace(tokenResult.Value))
        {
            logger.LogError(
                "Failed to obtain access token for provider {Provider}: {ErrorMessage}",
                provider.Value.ProviderName,
                tokenResult.Error
            );

            return Domain.Models.Result<CustodianRecord>.Fail(
                "Unable to obtain access token for fetch record."
            );
        }

        logger.LogInformation("Fetch Record access token obtained");

        var response = await providerClient.GetAsync(
            resolvedMapping.TargetUrl,
            tokenResult.Value,
            cancellationToken
        );

        if (!response.Success)
        {
            logger.LogError("Failed to fetch record: {Error}", response.Error);
            return Domain.Models.Result<CustodianRecord>.Fail(
                response.Error ?? "Error fetching record from provider."
            );
        }

        if (string.IsNullOrWhiteSpace(response.Value))
        {
            logger.LogInformation("Fetch Record return empty response");
            return Domain.Models.Result<CustodianRecord>.Fail("Requested record is empty");
        }

        var recordContent = JsonSerializer.Deserialize<CustodianRecord>(
            response.Value,
            JsonSerializerOptions.Web
        );

        return recordContent is not null
            ? Domain.Models.Result<CustodianRecord>.Ok(recordContent)
            : Domain.Models.Result<CustodianRecord>.Fail("Requested record is empty");
    }
}
