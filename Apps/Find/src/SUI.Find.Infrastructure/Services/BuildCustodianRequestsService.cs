using System.Text.Json;
using Microsoft.Extensions.Logging;
using SUI.Find.Application.Interfaces;
using SUI.Find.Application.Models;
using SUI.Find.Domain.Models;
using SUI.Find.Infrastructure.Interfaces;

namespace SUI.Find.Infrastructure.Services;

public class BuildCustodianRequestsService(
    ILogger<BuildCustodianRequestsService> logger,
    IBuildCustodianHttpRequest requestBuilder,
    IProviderHttpClient providerHttpClient,
    IOutboundAuthService outboundAuthService,
    IPersonIdEncryptionService encryptionService
) : IBuildCustodianRequestService
{
    public async Task<Result<List<SearchResultItem>>> GetSearchResultItemsFromCustodianAsync(
        BuildCustodianRequestDto data,
        CancellationToken cancellationToken
    )
    {
        if (data.Provider.Encryption is null)
        {
            logger.LogError(
                "Provider {ProviderDefinition} has no encryption configured",
                data.Provider
            );
            return Result<List<SearchResultItem>>.Fail("Provider has no encryption configured");
        }

        var encryptedPersonId = encryptionService.EncryptNhsToPersonId(
            data.Suid,
            data.Provider.Encryption
        );

        if (!encryptedPersonId.Success)
        {
            logger.LogError(
                "Encryption failed for provider {Provider}: {ErrorMessage}",
                data.Provider.ProviderName,
                encryptedPersonId.Error
            );

            return Result<List<SearchResultItem>>.Fail(
                encryptedPersonId.Error ?? "Encryption of PersonId Failed"
            );
        }

        var tokenResult = await outboundAuthService.GetAccessTokenAsync(
            data.Provider,
            CancellationToken.None
        );

        if (!tokenResult.Success || string.IsNullOrWhiteSpace(tokenResult.Value))
        {
            logger.LogError(
                "Failed to obtain access token for provider {Provider}: {ErrorMessage}",
                data.Provider.ProviderName,
                tokenResult.Error
            );

            return Result<List<SearchResultItem>>.Fail(
                tokenResult.Error ?? "Failed to obtain token"
            );
        }

        using var httpRequest = requestBuilder.BuildHttpRequest(
            data.Provider,
            encryptedPersonId.Value!,
            tokenResult.Value
        );

        var responseResult = await providerHttpClient.SendAsync(httpRequest, cancellationToken);

        if (!responseResult.Success)
        {
            logger.LogInformation(
                "Provider '{Provider}' returned error {ErrorMessage}",
                data.Provider.ProviderName,
                responseResult.Error
            );

            return Result<List<SearchResultItem>>.Fail(
                responseResult.Error ?? "Response from provider unsuccessful"
            );
        }

        if (string.IsNullOrWhiteSpace(responseResult.Value))
        {
            logger.LogInformation("Provider returned empty response");
            return Result<List<SearchResultItem>>.Fail("Empty response returned by provider");
        }

        var searchResultItems = JsonSerializer.Deserialize<List<SearchResultItem>>(
            responseResult.Value,
            JsonSerializerOptions.Web
        );

        if (searchResultItems is null || searchResultItems.Count == 0)
        {
            logger.LogInformation("No search result items found in service response");
            return Result<List<SearchResultItem>>.Fail("No search result items");
        }

        return Result<List<SearchResultItem>>.Ok(searchResultItems);
    }
}
