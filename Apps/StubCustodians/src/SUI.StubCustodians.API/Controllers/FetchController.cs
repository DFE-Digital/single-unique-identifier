using Asp.Versioning;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using SUI.Custodians.Domain.Models;
using SUI.StubCustodians.Application.Interfaces;
using SUI.StubCustodians.Application.Models;

namespace SUI.StubCustodians.API.Controllers;

[ApiController]
[Route("api/v{version:apiVersion}/[controller]")]
[ApiVersion("1.0")]
public class FetchController(ILogger<FetchController> logger, IRecordService recordService)
    : ControllerBase
{
    [HttpGet("{orgId}/{recordType}/{recordId}")]
    [RequiredScopes("fetch-record.read")]
    [ProducesResponseType(typeof(RecordEnvelope<SuiRecord>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemHttpResult), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetRecordEndpoint1(
        [FromRoute] string orgId,
        [FromRoute] string recordType,
        [FromRoute] string recordId
    )
    {
        return await ActionResult(orgId, recordId, recordType);
    }

    [HttpGet("{orgId}/{recordId}")]
    [RequiredScopes("fetch-record.read")]
    [ProducesResponseType(typeof(RecordEnvelope<SuiRecord>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemHttpResult), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetRecordEndpoint2(
        [FromRoute] string orgId,
        [FromRoute] string recordId,
        [FromQuery] string? recordType
    )
    {
        return await ActionResult(orgId, recordId, recordType);
    }

    private async Task<IActionResult> ActionResult(
        string orgId,
        string recordId,
        string? recordType
    )
    {
        logger.LogInformation("Getting record starting, for sui:'{Sui}'", recordId);

        switch (recordType)
        {
            case "education.details":
                var educationDetailsResult = await recordService.GetRecord<EducationDetailsRecord>(
                    recordId,
                    orgId
                );
                logger.LogInformation("Getting record ended, for sui:'{Sui}'", recordId);
                if (educationDetailsResult != null)
                    return Ok(educationDetailsResult);
                logger.LogInformation("Record not found, for sui:'{Sui}'", recordId);
                break;
            case "health.details":
                var healthDetailsResult = await recordService.GetRecord<HealthDataRecord>(
                    recordId,
                    orgId
                );
                logger.LogInformation("Getting record ended, for sui:'{Sui}'", recordId);
                if (healthDetailsResult != null)
                    return Ok(healthDetailsResult);
                logger.LogInformation("Record not found, for sui:'{Sui}'", recordId);
                break;
            case "crime-justice.details":
                var crimeDetailsResult = await recordService.GetRecord<CrimeDataRecord>(
                    recordId,
                    orgId
                );
                logger.LogInformation("Getting record ended, for sui:'{Sui}'", recordId);
                if (crimeDetailsResult != null)
                    return Ok(crimeDetailsResult);
                logger.LogInformation("Record not found, for sui:'{Sui}'", recordId);
                break;
            case "childrens-services.details":
                var childrensServicesDetailsResult =
                    await recordService.GetRecord<ChildrensServicesDetailsRecord>(recordId, orgId);
                logger.LogInformation("Getting record ended, for sui:'{Sui}'", recordId);
                if (childrensServicesDetailsResult != null)
                    return Ok(childrensServicesDetailsResult);
                logger.LogInformation("Record not found, for sui:'{Sui}'", recordId);
                break;
            case "personal.details":
                var personalDetailsResult = await recordService.GetRecord<PersonalDetailsRecord>(
                    recordId,
                    orgId
                );
                logger.LogInformation("Getting record ended, for sui:'{Sui}'", recordId);
                if (personalDetailsResult != null)
                    return Ok(personalDetailsResult);
                logger.LogInformation("Record not found, for sui:'{Sui}'", recordId);
                break;
        }

        return NotFound();
    }
}
