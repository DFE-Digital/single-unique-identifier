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
        string organisationId,
        CancellationToken ct
    )
    {
        var matchResult = await matchService.MatchPersonAsync(specification, ct);

        if (matchResult.TryPickT0(out var personId, out var remainder))
        {
            var getPersonIdResult = await HandlePersonIdRepresentationAsync(
                personId,
                organisationId
            );
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
        string organisationId
    )
    {
        var organisation = await custodianService.GetCustodianAsync(organisationId);
        if (!organisation.Success || organisation.Value is null)
        {
            logger.LogError(
                "Custodian organisation not found for organisation ID: {OrganisationId}",
                organisationId
            );
            return new Error();
        }

        return nhsPersonId.Value;
    }
}
