using Microsoft.Extensions.Logging;
using OneOf;
using OneOf.Types;
using SUI.Find.Application.Interfaces;
using SUI.Find.Application.Models;
using SUI.Find.Domain.ValueObjects;

namespace SUI.Find.Application.Services;

public interface IMatchingService
{
    Task<OneOf<EncryptedPersonId, NotFound, Error>> MatchPersonAsync(
        MatchPersonRequest request,
        string clientId
    );
}

public class MatchingService(
    ILogger<MatchingService> logger,
    IMatchRepository repository,
    ICustodianService custodianService,
    IPersonIdEncryptionService encryptionService
) : IMatchingService
{
    public async Task<OneOf<EncryptedPersonId, NotFound, Error>> MatchPersonAsync(
        MatchPersonRequest request,
        string clientId
    )
    {
        try
        {
            var org = await custodianService.GetCustodianAsync(clientId);
            if (!org.Success || org.Value is null)
            {
                logger.LogError(
                    "Custodian organisation not found for client ID: {ClientId}",
                    clientId
                );
                return new NotFound();
            }

            if (org.Value.Encryption is null)
            {
                logger.LogError(
                    "Encryption definition not found for custodian organisation: {OrgId}",
                    org.Value.OrgId
                );
                return new Error();
            }

            var response = await repository.MatchPersonAsync(request);
            return response.Match<OneOf<EncryptedPersonId, NotFound, Error>>(
                nhsNumber =>
                {
                    var encryptionResult = encryptionService.EncryptNhsToPersonId(
                        nhsNumber,
                        org.Value.Encryption
                    );

                    if (encryptionResult is not { Success: true, Value: not null })
                        return new Error();

                    return EncryptedPersonId.Create(encryptionResult.Value);
                },
                notFound => new NotFound(),
                error => new Error()
            );
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An error occurred while matching person.");
            return new Error();
        }
    }
}
