using Microsoft.Extensions.Logging;
using OneOf;
using OneOf.Types;
using SUI.GetAnIdentifier.Application.Constants;
using SUI.GetAnIdentifier.Application.Interfaces;
using SUI.GetAnIdentifier.Application.Models;
using SUI.GetAnIdentifier.Application.Models.Fhir;
using SUI.GetAnIdentifier.Application.Validation;

namespace SUI.GetAnIdentifier.Application.Services;

public class GetAnIdentifierService(
    ILogger<GetAnIdentifierService> logger,
    IFhirService fhirService
) : IGetAnIdentifierService
{
    public async Task<
        OneOf<GetAnIdentifierResult, DataQualityResult, NotFound, Error>
    > MatchPersonAsync(
        PersonSpecification request,
        string? correlationId = null,
        CancellationToken ct = default
    )
    {
        try
        {
            // 1. Validate the incoming request
            var validationResult = await new PersonSpecificationValidation().ValidateAsync(
                request,
                ct
            );
            var translatedResult = PersonDataQualityTranslator.Translate(request, validationResult);

            if (!translatedResult.hasMetRequirements)
            {
                return translatedResult.dataQualityResult;
            }

            // 2. Build the single PDS Query
            var searchQuery = BuildSearchQuery(request);

            // 3. Send directly to PDS
            var result = await fhirService.PerformSearchAsync(searchQuery, correlationId, ct);

            if (!result.Success)
            {
                logger.LogWarning("FHIR search encountered an error: {ErrorMessage}", result.Error);
                return new Error();
            }

            // 4. Evaluate the result and enforce the threshold
            if (
                result.Value is null
                || result.Value.Type != SearchResult.ResultType.Matched
                || string.IsNullOrWhiteSpace(result.Value.NhsNumber)
            )
            {
                logger.LogInformation("No confident match found or multiple matches returned.");
                return new NotFound();
            }

            var score = result.Value.Score.GetValueOrDefault();
            if (score < MatchScoreConstants.MinMatchThreshold)
            {
                logger.LogInformation(
                    "Match score {Score} was below the minimum threshold of {Threshold}.",
                    score,
                    MatchScoreConstants.MinMatchThreshold
                );
                return new NotFound();
            }

            // 5. Parse the NHS Number
            var nhsPersonId = NhsPersonId.Create(result.Value.NhsNumber);
            if (nhsPersonId is not { Success: true, Value: not null })
            {
                logger.LogError(
                    "Failed to create NhsPersonId from NHS number: {NhsNumber}",
                    result.Value.NhsNumber
                );
                return new Error();
            }

            // 6. Return the NHS Number and registered GP practice ODS code
            return new GetAnIdentifierResult(nhsPersonId.Value, result.Value.GeneralPractitioner);
        }
        catch (Exception ex)
        {
            logger.LogError(
                ex,
                "Unexpected error occurred when trying to match person: {Message}",
                ex.Message
            );
            return new Error();
        }
    }

    private static SearchQuery BuildSearchQuery(PersonSpecification request)
    {
        return new SearchQuery
        {
            // PDS requires Given and Birthdate as arrays. Birthdate requires the "eq" prefix.
            Given = string.IsNullOrWhiteSpace(request.Given) ? null : [request.Given],
            Family = string.IsNullOrWhiteSpace(request.Family) ? null : request.Family,
            Birthdate = request.BirthDate.HasValue
                ? [$"eq{request.BirthDate.Value:yyyy-MM-dd}"]
                : null,
            Gender = string.IsNullOrWhiteSpace(request.Gender) ? null : request.Gender,
            AddressPostalcode = string.IsNullOrWhiteSpace(request.AddressPostalCode)
                ? null
                : request.AddressPostalCode,
            Email = string.IsNullOrWhiteSpace(request.Email) ? null : request.Email,
            Phone = string.IsNullOrWhiteSpace(request.Phone) ? null : request.Phone,

            // Enable MOAB
            FuzzyMatch = true,
            ExactMatch = false,
        };
    }
}
