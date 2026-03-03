using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SUI.Find.Application.Configurations;
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
    IPersonIdEncryptionService encryptionService,
    IOptions<EncryptionConfiguration> encryptionConfig
) : IBuildCustodianRequestService
{
    public async Task<
        Result<List<CustodianSearchResultItem>>
    > GetSearchResultItemsFromCustodianAsync(
        BuildCustodianRequestDto data,
        CancellationToken cancellationToken
    )
    {
        var personId = string.Empty;
        var encrypt = encryptionConfig.Value.EnablePersonIdEncryption;
        if (data.Provider.Encryption is not null && encrypt)
        {
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

                return Result<List<CustodianSearchResultItem>>.Fail(
                    encryptedPersonId.Error ?? "Encryption of PersonId Failed"
                );
            }

            if (encryptedPersonId.Value != null)
                personId = encryptedPersonId.Value;
        }
        else
        {
            personId = data.Suid;
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

            return Result<List<CustodianSearchResultItem>>.Fail(
                tokenResult.Error ?? "Failed to obtain token"
            );
        }

        using var httpRequest = requestBuilder.BuildHttpRequest(
            data.Provider,
            personId,
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

            return Result<List<CustodianSearchResultItem>>.Fail(
                responseResult.Error ?? "Response from provider unsuccessful"
            );
        }

        if (string.IsNullOrWhiteSpace(responseResult.Value))
        {
            logger.LogInformation("Provider returned empty response");
            return Result<List<CustodianSearchResultItem>>.Fail(
                "Empty response returned by provider"
            );
        }

        var searchResultItems = JsonSerializer.Deserialize<List<SearchResultItem>>(
            responseResult.Value,
            JsonSerializerOptions.Web
        );

        if (searchResultItems is null || searchResultItems.Count == 0)
        {
            logger.LogInformation("No search result items found in service response");
            return Result<List<CustodianSearchResultItem>>.Fail("No search result items");
        }

        return Result<List<CustodianSearchResultItem>>.Ok(
            searchResultItems
                .Select(searchResultItem =>
                    CustodianSearchResultItem.Create(
                        data.Provider.OrgId,
                        data.Provider.OrgName,
                        searchResultItem
                    )
                )
                .ToList()
        );
    }
}
