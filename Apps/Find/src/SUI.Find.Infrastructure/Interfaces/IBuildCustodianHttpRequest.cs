using SUI.Find.Application.Models;

namespace SUI.Find.Infrastructure.Interfaces;

public interface IBuildCustodianHttpRequest
{
    HttpRequestMessage BuildHttpRequest(
        ProviderDefinition provider,
        string personId,
        string? bearerToken
    );
}
