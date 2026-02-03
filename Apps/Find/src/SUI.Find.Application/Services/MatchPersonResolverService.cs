using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OneOf;
using OneOf.Types;
using SUI.Find.Application.Configurations;
using SUI.Find.Application.Interfaces;
using SUI.Find.Application.Interfaces.Matching;
using SUI.Find.Application.Models.Matching;
using SUI.Find.Domain.ValueObjects;

namespace SUI.Find.Application.Services;

/// <inheritdoc />
public class MatchPersonOrchestrationService(
    ILogger<MatchPersonOrchestrationService> logger,
    IMatchingService matchService,
    ICustodianService custodianService,
    IPersonIdEncryptionService encryptionService,
    IOptions<EncryptionConfiguration> encryptionConfig
) : IMatchPersonOrchestrationService
{
    public async Task<OneOf<PersonIdValue, DataQualityResult, NotFound, Error>> FindPersonIdAsync(
        PersonSpecification specification,
        string clientId,
        CancellationToken ct
    )
    {
        var matchResult = await matchService.MatchPersonAsync(specification, ct);

        if (matchResult.TryPickT0(out var personId, out var remainder))
        {
            var getPersonIdResult = await HandlePersonIdRepresentationAsync(personId, clientId);
            return getPersonIdResult.Match<
                OneOf<PersonIdValue, DataQualityResult, NotFound, Error>
            >(encryptedId => encryptedId, error => error);
        }

        return remainder.Match<OneOf<PersonIdValue, DataQualityResult, NotFound, Error>>(
            dataQuality => dataQuality,
            notFound => notFound,
            error => error
        );
    }

    private async Task<OneOf<PersonIdValue, Error>> HandlePersonIdRepresentationAsync(
        NhsPersonId nhsPersonId,
        string clientId
    )
    {
        var encrypt = encryptionConfig.Value.EnablePersonIdEncryption;

        var client = await custodianService.GetCustodianAsync(clientId);
        if (!client.Success || client.Value is null)
        {
            logger.LogError("Custodian organisation not found for client ID: {ClientId}", clientId);
            return new Error();
        }

        // IF global on and have encryption key, then encrypt
        var clientEncryptionKeyExists = !string.IsNullOrEmpty(client.Value.Encryption?.Key);
        if (clientEncryptionKeyExists && encrypt)
        {
            var encryptionResult = encryptionService.EncryptNhsToPersonId(
                nhsPersonId.Value,
                client.Value.Encryption!
            );

            if (encryptionResult is not { Success: true, Value: not null })
            {
                logger.LogError(
                    "Failed to encrypt NHS number to PersonId for NHS number: {NhsNumber}",
                    nhsPersonId.Value
                );
                return new Error();
            }

            return new EncryptedSuidPersonId(encryptionResult.Value);
        }

        return new PlainPersonId(nhsPersonId.Value);
    }
}

public abstract record PersonIdValue(string Value);

public sealed record PlainPersonId(string Value) : PersonIdValue(Value);

public sealed record EncryptedSuidPersonId(string Value) : PersonIdValue(Value);
