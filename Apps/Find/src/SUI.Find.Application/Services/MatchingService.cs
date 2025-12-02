using Microsoft.Extensions.Logging;
using SUI.Find.Application.Interfaces;
using SUI.Find.Application.Models;
using SUI.Find.Domain.Models;
using SUI.Find.Domain.ValueObjects;

namespace SUI.Find.Application.Services;

public interface IMatchingService
{
    Task<MatchPersonResponse> MatchPersonAsync(MatchPersonRequest request, string clientId);
}

public class MatchingService(
    ILogger<MatchingService> logger,
    IMatchRepository repository,
    ICustodianService custodianService,
    IPersonIdEncryptionService encryptionService
) : IMatchingService
{
    public async Task<MatchPersonResponse> MatchPersonAsync(
        MatchPersonRequest request,
        string clientId
    )
    {
        var orgs = await custodianService.GetCustodiansAsync();
        var thisOrg = orgs.FirstOrDefault(o => o.OrgId == clientId);
        if (thisOrg?.Encryption is null)
        {
            logger.LogError("Custodian organisation not found for client ID: {ClientId}", clientId);
            return new MatchPersonResponse.Error("Custodian organisation not found.");
        }

        var response = await repository.MatchPersonAsync(request);
        return response switch
        {
            MatchFhirResponse.NoMatch => new MatchPersonResponse.NoMatch(),
            MatchFhirResponse.Error error => new MatchPersonResponse.Error(error.ErrorMessage),
            MatchFhirResponse.Match match => HandleEncryptPersonIdResult(
                match.NhsNumber,
                thisOrg.Encryption
            ),
            _ => new MatchPersonResponse.Error("Unknown response from match repository."),
        };
    }

    private MatchPersonResponse HandleEncryptPersonIdResult(
        string personId,
        EncryptionDefinition encryptionDefinition
    )
    {
        var encryptionResult = encryptionService.EncryptNhsToPersonId(
            personId,
            encryptionDefinition
        );

        if (encryptionResult is not { Success: true, Value: not null })
            return new MatchPersonResponse.Error("Failed to encrypt person ID.");

        var val = new EncryptedPersonId(encryptionResult.Value);
        return new MatchPersonResponse.Match(val);
    }
}
