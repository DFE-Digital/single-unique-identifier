using Hl7.Fhir.Model;
using Hl7.Fhir.Rest;
using Microsoft.Extensions.Logging;
using SUI.GetAnIdentifier.Application.Interfaces;
using SUI.GetAnIdentifier.Application.Models;
using SUI.GetAnIdentifier.Application.Models.Fhir;
using SUI.GetAnIdentifier.Infrastructure.Factories;
using SUI.GetAnIdentifier.Infrastructure.Interfaces;

namespace SUI.GetAnIdentifier.Infrastructure.Services;

public class FhirService(ILogger<FhirService> logger, IFhirClientFactory fhirClientFactory)
    : IFhirService
{
    public async Task<Result<SearchResult>> PerformSearchAsync(
        SearchQuery searchQuery,
        string? correlationId = null,
        CancellationToken ct = default
    )
    {
        try
        {
            var client = await fhirClientFactory.CreateFhirClientAsync(correlationId, ct);
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
        catch (FhirOperationException ex)
        {
            // Handle NHS Digital Non-Success Responses (e.g. 400, 500)
            if (ex.Outcome != null && ex.Outcome.Issue.Count != 0)
            {
                var issues = string.Join(
                    " | ",
                    ex.Outcome.Issue.Select(i =>
                        $"Severity: {i.Severity}, Code: {i.Code}, Diagnostics: {i.Diagnostics}"
                    )
                );

                logger.LogError(
                    ex,
                    "PDS API returned an OperationOutcome error. Status: {StatusCode}, Issues: {Issues}",
                    ex.Status,
                    issues
                );
            }
            else
            {
                logger.LogError(
                    ex,
                    "PDS API returned a non-success response. Status: {StatusCode}",
                    ex.Status
                );
            }

            return Result<SearchResult>.Fail($"PDS API Error: {ex.Status}");
        }
        catch (TaskCanceledException ex) when (!ct.IsCancellationRequested)
        {
            // Handle Downstream Timeouts
            logger.LogError(ex, "Request to PDS API timed out.");
            return Result<SearchResult>.Fail("PDS API Timeout");
        }
        catch (HttpRequestException ex)
        {
            // Handle DNS/Network level failures
            logger.LogError(ex, "Network error while connecting to PDS API.");
            return Result<SearchResult>.Fail("PDS API Network Error");
        }
        catch (Exception ex)
        {
            // Catch-all
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

        var generalPractitioner =
            (entry.Resource as Patient)
                ?.GeneralPractitioner.Select(reference => reference.Identifier?.Value)
                .Where(value => !string.IsNullOrWhiteSpace(value))
                .Select(value => value!)
                .ToArray()
            ?? [];

        return Result<SearchResult>.Ok(
            SearchResult.Match(entry.Resource.Id, entry.Search.Score, generalPractitioner)
        );
    }
}
