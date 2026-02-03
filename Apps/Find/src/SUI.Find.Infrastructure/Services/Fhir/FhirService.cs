using Hl7.Fhir.Model;
using Microsoft.Extensions.Logging;
using SUI.Find.Application.Interfaces.Matching;
using SUI.Find.Application.Models.Fhir;
using SUI.Find.Domain.Models;
using SUI.Find.Infrastructure.Factories.Fhir;
using SUI.Find.Infrastructure.Interfaces.Fhir;

namespace SUI.Find.Infrastructure.Services.Fhir;

public class FhirService(ILogger<FhirService> logger, IFhirClientFactory fhirClientFactory)
    : IFhirService
{
    public async Task<Result<SearchResult>> PerformSearchAsync(
        SearchQuery searchQuery,
        CancellationToken ct
    )
    {
        try
        {
            var client = await fhirClientFactory.CreateFhirClientAsync(ct);
            var searchParams = SearchParamsFactory.Create(searchQuery);

            logger.LogInformation("Searching for NHS patient record...");

            var bundle = await client.SearchAsync<Patient>(searchParams, ct);

            if (bundle is null)
            {
                var isMultiMatch =
                    client.LastBodyAsResource is OperationOutcome outcome
                    && outcome.Issue.Any(i => i.Code == OperationOutcome.IssueType.MultipleMatches);
                logger.LogInformation(
                    "Handling null bundle from FHIR API, isMultiMatch: {IsMultiMatch}",
                    isMultiMatch
                );
                return isMultiMatch
                    ? Result<SearchResult>.Ok(SearchResult.MultiMatched())
                    : Result<SearchResult>.Fail("FHIR API returned null bundle");
            }

            logger.LogInformation(
                "Handling bundle with {EntryCount} entries from FHIR API",
                bundle.Entry.Count
            );

            return bundle.Entry.Count switch
            {
                0 => Result<SearchResult>.Ok(SearchResult.Unmatched()),
                1 => HandleSingleEntry(bundle.Entry[0]),
                _ => Result<SearchResult>.Fail("Unexpected multiple entries"),
            };
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error occurred while performing FHIR search");
            return Result<SearchResult>.Fail(ex.Message);
        }
    }

    private static Result<SearchResult> HandleSingleEntry(Bundle.EntryComponent entry)
    {
        if (entry.Resource?.Id is null)
        {
            return Result<SearchResult>.Fail("FHIR API returned missing Resource or Id");
        }

        if (entry.Search is null)
        {
            return Result<SearchResult>.Fail(
                "FHIR API returned missing Search required to get the score"
            );
        }

        return Result<SearchResult>.Ok(SearchResult.Match(entry.Resource.Id, entry.Search.Score));
    }
}
