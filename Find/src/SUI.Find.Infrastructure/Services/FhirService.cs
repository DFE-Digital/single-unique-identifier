using Hl7.Fhir.Model;
using Hl7.Fhir.Rest;
using Microsoft.Extensions.Logging;
using SUI.Find.Application.Common;
using SUI.Find.Application.Interfaces;
using SUI.Find.Application.Models;
using SUI.Find.Infrastructure.Interfaces;

namespace SUI.Find.Infrastructure.Services;

/// <summary>
/// Infrastructure code class for calling FHIR endpoint.
/// </summary>
public class FhirService(ILogger<FhirService> logger, IFhirClientFactory fhirClientFactory) : IFhirService
{
    public async Task<Result<SearchResult>> PerformSearchAsync(SearchQuery searchQuery)
    {
        try
        {
            var client = fhirClientFactory.CreateFhirClient();
            // var searchParams = SearchParamsFactory.Create(searchQuery);
            var searchParams = new SearchParams();

            logger.LogInformation("Searching for NHS patient record...");

            var bundle = await client.SearchAsync<Patient>(searchParams);

            if (bundle is null)
            {
                var isMultiMatch = client.LastBodyAsResource is OperationOutcome outcome &&
                                   outcome.Issue.Any(i => i.Code == OperationOutcome.IssueType.MultipleMatches);

                return isMultiMatch
                    ? Result<SearchResult>.Success(SearchResult.MultiMatched())
                    : Result<SearchResult>.Failure("FHIR API returned null bundle");
            }

            return bundle.Entry.Count switch
            {
                0 => Result<SearchResult>.Success(SearchResult.Unmatched()),
                1 => Result<SearchResult>.Success(
                    SearchResult.Match(
                        bundle.Entry[0].Resource.Id,
                        bundle.Entry[0].Search.Score
                    )
                ),
                _ => Result<SearchResult>.Failure("Unexpected multiple entries")
            };
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error occurred while performing FHIR search");
            return Result<SearchResult>.Failure(ex.Message);
        }
    }
}