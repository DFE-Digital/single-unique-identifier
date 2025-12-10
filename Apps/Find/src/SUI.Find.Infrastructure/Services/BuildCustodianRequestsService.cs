using SUI.Find.Application.Interfaces;
using SUI.Find.Application.Models;
using SUI.Find.Domain.Models;
using SUI.Find.Infrastructure.Interfaces;

namespace SUI.Find.Infrastructure.Services;

public class BuildCustodianRequestsService(IBuildCustodianHttpRequest requestBuilder, IProviderHttpClient providerHttpClient) : IBuildCustodianRequestService
{
    public async Task<Result<string>> BuildCustodianRequestAsync(BuildCustodianRequestDto request, CancellationToken cancellationToken)
    {
        using var httpRequest = requestBuilder.BuildHttpRequest(
            request.Provider,
            request.EncryptedPersonId,
            request.AccessToken
        );

        var responseResult = await providerHttpClient.SendAsync(httpRequest, cancellationToken);

        return responseResult;
    }
}