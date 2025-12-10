using SUI.Find.Application.Models;

namespace SUI.Find.Application.Interfaces;

public interface IBuildCustodianHttpRequest
{
    HttpRequestMessage BuildHttpRequest(ProviderDefinition provider, string encryptedPersonId, string? bearerToken);
}