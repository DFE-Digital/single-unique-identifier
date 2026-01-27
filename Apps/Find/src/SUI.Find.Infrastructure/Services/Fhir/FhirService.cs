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
    public async Task<Result<SearchResult>> PerformSearchAsync(SearchQuery searchQuery)
    {
        try
        {
            var client = await fhirClientFactory.CreateFhirClientAsync();
            var searchParams = SearchParamsFactory.Create(searchQuery);

            logger.LogInformation("Searching for NHS patient record...");

            var bundle = await client.SearchAsync<Patient>(searchParams);

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
                1 => Result<SearchResult>.Ok(
                    SearchResult.Match(bundle.Entry[0].Resource.Id, bundle.Entry[0].Search.Score)
                ),
                _ => Result<SearchResult>.Fail("Unexpected multiple entries"),
            };
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error occurred while performing FHIR search");
            return Result<SearchResult>.Fail(ex.Message);
        }
    }
}
