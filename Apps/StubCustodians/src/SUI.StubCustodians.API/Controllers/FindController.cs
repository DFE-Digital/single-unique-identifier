using System.Diagnostics.CodeAnalysis;
using Asp.Versioning;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using SUI.StubCustodians.Application.Common;
using SUI.StubCustodians.Application.Interfaces;
using SUI.StubCustodians.Application.Models;

namespace SUI.StubCustodians.API.Controllers;

[ExcludeFromCodeCoverage]
[ApiController]
[Route("api/v{version:apiVersion}/[controller]")]
[ApiVersion("1.0")]
public class FindController(ILogger<FindController> logger, IManifestService manifestService)
    : ControllerBase
{
    [HttpGet("{orgId}/{personId}")]
    [RequiredScopes("find-record.read")]
    [ProducesResponseType(typeof(IList<SearchResultItem>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(FailureInfo), StatusCodes.Status400BadRequest)]
    public async Task<Results<Ok<IList<SearchResultItem>>, ProblemHttpResult>> GetManifest(
        [FromRoute] string orgId,
        [FromRoute] string personId,
        [FromQuery] string? recordType,
        CancellationToken cancellationToken
    )
    {
        logger.LogInformation("Getting manifest starting, for personId:'{personId}'", personId);
        return await Manifest(orgId, personId, recordType, cancellationToken);
    }

    [HttpPost("manifest")]
    [RequiredScopes("find-record.read")]
    [ProducesResponseType(typeof(IEnumerable<SearchResultItem>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(FailureInfo), StatusCodes.Status400BadRequest)]
    public async Task<Results<Ok<IList<SearchResultItem>>, ProblemHttpResult>> PostManifest(
        [FromBody] ManifestRequest request,
        CancellationToken cancellationToken
    )
    {
        logger.LogInformation(
            "Getting manifest starting, for personId:'{personId}'",
            request.PersonId
        );
        return await Manifest(
            request.OrgId,
            request.PersonId,
            request.RecordType,
            cancellationToken
        );
    }

    public sealed record ManifestRequest(string OrgId, string PersonId, string? RecordType);

    private async Task<Results<Ok<IList<SearchResultItem>>, ProblemHttpResult>> Manifest(
        string orgId,
        string personId,
        string? recordType,
        CancellationToken cancellationToken
    )
    {
        try
        {
            var result = await manifestService.GetManifestForOrganisation(
                orgId,
                personId,
                GetBaseUrl(HttpContext.Request),
                cancellationToken,
                recordType
            );
            return TypedResults.Ok(result);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            logger.LogWarning("Operation cancelled");
            return TypedResults.Problem("Operation Cancelled", statusCode: 499);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An error occurred while retrieving manifest");
            return TypedResults.Problem(ex.Message, statusCode: 500);
        }
    }

    private static string GetBaseUrl(HttpRequest req)
    {
        var host = req.Headers.TryGetValue("X-Forwarded-Host", out var fwdHost)
            ? fwdHost.FirstOrDefault()
            : req.Host.Host;

        var proto = req.Headers.TryGetValue("X-Forwarded-Proto", out var fwdProto)
            ? fwdProto.FirstOrDefault()
            : req.Scheme;

        return $"{proto}://{host}:{req.Host.Port}";
    }
}
