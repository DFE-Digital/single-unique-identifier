using System.Text.Json;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using SUI.Find.Application.Constants;
using SUI.Find.Application.Dtos;
using SUI.Find.Application.Interfaces;
using SUI.Find.Application.Models;
using SUI.Find.FindApi.Utility;
using SUI.Find.Infrastructure.Utility;

namespace SUI.Find.FindApi.Functions.ProviderTriggers;

// TODO this needs separating according to clean architecture principles
public class QueryProvidersFunction(
    ILogger<QueryProvidersFunction> logger,
    IHttpClientFactory httpClientFactory,
    IPersonIdEncryptionService encryptionService,
    IMaskUrlService maskUrlService,
    IOutboundAuthService outboundAuthService
)
{
    [Function(nameof(QueryProvidersFunction))]
    public async Task<IReadOnlyList<SearchResultItem>> QueryProvider(
        [ActivityTrigger] FunctionContext context,
        QueryProviderInput data
    )
    {
        using var logScope = logger.BeginScope("CorrelationId: {CorrelationId}", data.InvocationId);
        logger.LogInformation("Query Provider triggered");

        if (data?.Provider.Encryption is null)
        {
            throw new InvalidOperationException(
                $"Provider '{data?.Provider.OrgId}' has no encryption configured."
            );
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

            return [];
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

            return [];
        }
        logger.LogInformation("Query Provider access token obtained");

        using var request = BuildCustodianHttpRequest.BuildHttpRequest(
            data.Provider,
            encryptedPersonId.Value!,
            tokenResult.Value
        );

        using var httpClient = httpClientFactory.CreateClient(
            ApplicationConstants.Providers.LoggingName
        );
        using var response = await httpClient.SendAsync(request, context.CancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            logger.LogInformation(
                "Provider '{Provider}' returned status code {StatusCode}",
                data.Provider.ProviderName,
                response.StatusCode
            );

            return [];
        }

        var responseContent = await response.Content.ReadAsStringAsync(context.CancellationToken);

        if (string.IsNullOrWhiteSpace(responseContent))
        {
            logger.LogInformation("Provider returned empty response");

            return [];
        }

        var searchResultItems = JsonSerializer.Deserialize<List<SearchResultItem>>(
            responseContent,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
        );

        if (searchResultItems is null || searchResultItems.Count == 0)
        {
            logger.LogInformation("No search result items found in provider response");
            return [];
        }

        var maskedSearchResultItems = await maskUrlService.CreateAsync(
            searchResultItems,
            data,
            context.CancellationToken
        );

        logger.LogInformation("Query Provider request completed");

        return maskedSearchResultItems;
    }
}
