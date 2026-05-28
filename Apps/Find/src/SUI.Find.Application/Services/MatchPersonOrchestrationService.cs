using Microsoft.Extensions.Logging;
using OneOf;
using OneOf.Types;
using SUI.Find.Application.Interfaces;
using SUI.Find.Application.Interfaces.Matching;
using SUI.Find.Application.Models.Matching;
using SUI.Find.Domain.ValueObjects;

namespace SUI.Find.Application.Services;

/// <inheritdoc />
public class MatchPersonOrchestrationService(
    ILogger<MatchPersonOrchestrationService> logger,
    IMatchingService matchService,
    ICustodianService custodianService
) : IMatchPersonOrchestrationService
{
    public async Task<OneOf<string, DataQualityResult, NotFound, Error>> FindPersonIdAsync(
        PersonSpecification specification,
        string clientId,
        CancellationToken ct
    )
    {
        var matchResult = await matchService.MatchPersonAsync(specification, ct);

        if (matchResult.TryPickT0(out var personId, out var remainder))
        {
            var getPersonIdResult = await HandlePersonIdRepresentationAsync(personId, clientId);
            return getPersonIdResult.Match<OneOf<string, DataQualityResult, NotFound, Error>>(
                plainId => plainId,
                error => error
            );
        }

        return remainder.Match<OneOf<string, DataQualityResult, NotFound, Error>>(
            dataQuality => dataQuality,
            notFound => notFound,
            error => error
        );
    }

    private async Task<OneOf<string, Error>> HandlePersonIdRepresentationAsync(
        NhsPersonId nhsPersonId,
        string clientId
    )
    {
        var client = await custodianService.GetCustodianAsync(clientId);
        if (!client.Success || client.Value is null)
        {
            logger.LogError("Custodian organisation not found for client ID: {ClientId}", clientId);
            return new Error();
        }

        return nhsPersonId.Value;
    }
}
