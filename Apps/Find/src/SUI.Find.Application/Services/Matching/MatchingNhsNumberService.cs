using Microsoft.Extensions.Logging;
using OneOf;
using OneOf.Types;
using SUI.Find.Application.Factories.PdsSearch;
using SUI.Find.Application.Interfaces.Matching;
using SUI.Find.Application.Models.Fhir;
using SUI.Find.Application.Models.Matching;
using SUI.Find.Application.Validation.Matching;
using SUI.Find.Domain.ValueObjects;

namespace SUI.Find.Application.Services.Matching;

public class MatchingNhsNumberService(
    ILogger<MatchingNhsNumberService> logger,
    IPdsSearchFactory searchFactory,
    IFhirService fhirService
) : IMatchingNhsNumberService
{
    public async Task<OneOf<NhsPersonId, NotFound, Error>> MatchPersonAsync(
        PersonSpecification request,
        string clientId, // To know who is making the request
        CancellationToken ct
    )
    {
        var validationResult = await new PersonSpecificationValidation().ValidateAsync(request, ct);
        if (!validationResult.IsValid)
        {
            return new Error();
        }

        var searchQueries = BuildSearchQueries(request);

        var result = await PerformSearchAsync(searchQueries, ct);

        return result.Match<OneOf<NhsPersonId, NotFound, Error>>(
            nhsPersonId => nhsPersonId,
            _ =>
            {
                logger.LogInformation("No confident match found for person specification");
                return new NotFound();
            }
        );
    }

    private OrderedDictionary<string, SearchQuery> BuildSearchQueries(PersonSpecification request)
    {
        // Version number could come from config if we need to support multiple versions
        var searchStrategy = searchFactory.GetVersion(1);
        return searchStrategy.BuildQuery(request);
    }

    private async Task<OneOf<NhsPersonId, None>> PerformSearchAsync(
        OrderedDictionary<string, SearchQuery> searchQueries,
        CancellationToken cancellationToken
    )
    {
        decimal bestScore = 0;
        SearchResult? searchResult = null;
        // We want to keep it synchronous so we can stop as soon as we get a confident match
        foreach (var query in searchQueries)
        {
            var result = await fhirService.PerformSearchAsync(query.Value);

            if (!result.Success)
                continue;

            searchResult = result.Value;

            if (searchResult is not null && bestScore > searchResult.Score)
            {
                bestScore = searchResult.Score ?? 0;
            }
        }

        logger.LogInformation(
            "Performed FHIR search: final score: {Score}",
            searchResult?.Score ?? 0
        );

        if (bestScore >= 0.95m && searchResult?.NhsNumber is not null)
        {
            var nhsPersonIdResult = NhsPersonId.Create(searchResult.NhsNumber);
            if (nhsPersonIdResult is { Success: true, Value: not null })
            {
                return nhsPersonIdResult.Value;
            }
        }

        return new None();
    }
}
