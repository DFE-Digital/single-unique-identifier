using System.Text.Json;
using Microsoft.Extensions.Logging;
using SUI.Find.Application.Dtos;
using SUI.Find.Application.Interfaces;
using SUI.Find.Application.Models;
using SUI.Find.Domain.Models;

namespace SUI.Find.Application.Services;

public class QueryProvidersService(
    IProviderHttpClient providerHttpClient,
    IBuildCustodianHttpRequest buildCustodianHttpRequest,
    ILogger<QueryProvidersService> logger,
    IPersonIdEncryptionService encryptionService,
    IMaskUrlService maskUrlService,
    IOutboundAuthService outboundAuthService
    ) : IQueryProvidersService
{
    public async Task<Result<IReadOnlyList<SearchResultItem>>> QueryProvidersAsync(QueryProviderInput data, CancellationToken cancellationToken)
    {
        if (data?.Provider.Encryption is null)
        {
            logger.LogError($"Provider {data?.Provider} has no encryption configured");
            return Result<IReadOnlyList<SearchResultItem>>.Fail("Provider has no encryption configured");
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

            return Result<IReadOnlyList<SearchResultItem>>.Fail(encryptedPersonId.Error ?? "Encryption of PersonId Failed");
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

            return Result<IReadOnlyList<SearchResultItem>>.Fail(tokenResult.Error ?? "Failed to obtain token");
        }
        logger.LogInformation("Query Provider access token obtained");

        using var request = buildCustodianHttpRequest.BuildHttpRequest(
            data.Provider,
            encryptedPersonId.Value!,
            tokenResult.Value
        );

        var responseResult = await providerHttpClient.SendAsync(request, cancellationToken);

        if (!responseResult.Success)
        {
            logger.LogInformation(
                "Provider '{Provider}' returned error {ErrorMessage}",
                data.Provider.ProviderName,
                responseResult.Error
            );

            return Result<IReadOnlyList<SearchResultItem>>.Fail(responseResult.Error ?? "Response from provider unsuccessful");
        }

        if (string.IsNullOrWhiteSpace(responseResult.Value))
        {
            logger.LogInformation("Provider returned empty response");
            return Result<IReadOnlyList<SearchResultItem>>.Fail("Empty response returned by provider");
        }

        var searchResultItems = JsonSerializer.Deserialize<List<SearchResultItem>>(
            responseResult.Value,
            JsonSerializerOptions.Web
        );

        if (searchResultItems is null || searchResultItems.Count == 0)
        {
            logger.LogInformation("No search result items found in service response");
            return Result<IReadOnlyList<SearchResultItem>>.Fail("No search result items");
        }

        var maskedSearchResultItems = await maskUrlService.CreateAsync(
            searchResultItems,
            data,
            cancellationToken
        );

        logger.LogInformation("Query Provider Service Returning Items");
        
        return Result<IReadOnlyList<SearchResultItem>>.Ok(maskedSearchResultItems);

    }
}