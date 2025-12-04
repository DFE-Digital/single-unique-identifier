using System.Text.Json;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using SUI.Find.Application.Dtos;
using SUI.Find.Application.Interfaces;
using SUI.Find.Application.Models;
using SUI.Find.FindApi.Utility;

namespace SUI.Find.FindApi.Functions.ProviderTriggers;
// TODO confirm if encrypted personid is passed in url

public class QueryProvidersFunction(
    ILogger<QueryProvidersFunction> logger,
    IHttpClientFactory httpClientFactory,
    IPersonIdEncryptionService encryptionService,
    IMaskUrlService maskUrlService)
{
    [Function(nameof(QueryProvidersFunction))]
    public async Task<IReadOnlyList<SearchResultItem>> QueryProvider(
        [ActivityTrigger] FunctionContext context,
        QueryProviderInput data
    )
    {
        var (
            clientId, instanceId, invocationId, suid, provider
            ) = data;

        using var logScope = logger.BeginScope("CorrelationId: {CorrelationId}", invocationId);
        logger.LogInformation("Query Provider triggered");

        if (provider.Encryption is null)
        {
            throw new InvalidOperationException($"Provider '{provider.OrgId}' has no encryption configured.");
        }

        var encryptedPersonId = encryptionService.EncryptNhsToPersonId(suid, provider.Encryption);
        var authConnection = provider.Connection.Auth;
        var bearerToken = authConnection is null
            ? null
            : "test-bearer-token";

        if (!encryptedPersonId.Success)
        {
            logger.LogError("Encryption failed for provider {Provider}: {ErrorMessage}",
                provider.ProviderName,
                encryptedPersonId.Error);

            return [];
        }

        using var request = BuildCustodianHttpRequest.BuildHttpRequest(provider, encryptedPersonId.Value!, bearerToken);

        using var httpClient = httpClientFactory.CreateClient("providers");
        using var response = await httpClient.SendAsync(request, context.CancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            logger.LogInformation("Provider '{Provider}' returned status code {StatusCode}", provider.ProviderName,
                response.StatusCode);

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
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });


        if (searchResultItems is null || searchResultItems.Count == 0)
        {
            logger.LogInformation("No search result items found in provider response");
            return [];
        }

        var maskedSearchResultItems = await maskUrlService.CreateAsync(searchResultItems, data, context.CancellationToken);

        logger.LogInformation("Query Provider request completed");

        return maskedSearchResultItems;
    }
}




