using System.Text.Json;
using Microsoft.Extensions.Logging;
using SUI.Find.Application.Interfaces;
using SUI.Find.Domain.Models;
using SUI.Find.Application.Models;

namespace SUI.Find.Application.Services;

public class FetchRecordService(ILogger<FetchRecordService> logger, IMaskUrlService maskUrlService, ICustodianService custodianService, IProviderHttpClient providerClient, IOutboundAuthService outboundAuthService) : IFetchRecordService
{
    public async Task<Result<RecordBase>> FetchRecordAsync(string fetchId, string requestingOrgId, CancellationToken cancellationToken)
    {
        var resolvedMapping = await maskUrlService.ResolveAsync(requestingOrgId, fetchId, cancellationToken);

        if (!resolvedMapping.Success || resolvedMapping.Value is null)
        {
            logger.LogError("Failed to resolve fetch mapping for ID {FetchId} and Requesting Org {RequestingOrgId}.",
                fetchId, requestingOrgId);
            return Result<RecordBase>.Fail(resolvedMapping.Error ?? "Failed to resolve fetch mapping.");
        }

        var provider = await custodianService.GetCustodianAsync(resolvedMapping.Value.TargetOrgId);

        if (!provider.Success || provider.Value is null)
        {
            logger.LogError("Failed to retrieve custodian organisation for Org ID {OrgId}",
                resolvedMapping.Value.TargetOrgId);
            return Result<RecordBase>.Fail(provider.Error ?? "Failed to retrieve custodian organisation.");
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

            return Result<RecordBase>.Fail("Unable to obtain access token for fetch record.");
        }

        logger.LogInformation("Fetch Record access token obtained");


        var response = await providerClient.GetAsync(
            resolvedMapping.Value.TargetUrl,
            tokenResult.Value,
            cancellationToken
        );

        if (!response.Success)
        {
            logger.LogError("Failed to fetch record: {Error}", response.Error);
            return Result<RecordBase>.Fail(response.Error ?? "Error fetching record from provider.");
        }

        if (string.IsNullOrWhiteSpace(response.Value))
        {
            logger.LogInformation("Fetch Record return empty response");
            return Result<RecordBase>.Fail("Fetched record is empty");
        }

        var recordContent = JsonSerializer.Deserialize<RecordBase>(
            response.Value,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        return Result<RecordBase>.Ok(recordContent);
    }
}