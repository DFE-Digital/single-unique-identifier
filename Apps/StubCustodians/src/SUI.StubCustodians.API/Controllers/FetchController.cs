using System.Diagnostics.CodeAnalysis;
using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;
using SUI.Custodians.Domain.Models;
using SUI.StubCustodians.Application.Common;
using SUI.StubCustodians.Application.Interfaces;
using SUI.StubCustodians.Application.Models;

namespace SUI.StubCustodians.API.Controllers;

[ExcludeFromCodeCoverage]
[ApiController]
[Route("api/v{version:apiVersion}/[controller]")]
[ApiVersion("1.0")]
public class FetchController(ILogger<RecordsController> logger, IRecordService recordService)
    : ControllerBase
{
    [HttpGet("{providerSystemId}/{recordType}/{sui}")]
    [RequiredScopes("fetch-record.read")]
    [ProducesResponseType(typeof(RecordEnvelope<SuiRecord>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(FailureInfo), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetRecord(
        [FromRoute] string sui,
        [FromRoute] string recordType,
        [FromRoute] string providerSystemId
    )
    {
        return await ActionResult(sui, recordType, providerSystemId);
    }

    [HttpGet("{providerSystemId}/{sui}")]
    [RequiredScopes("fetch-record.read")]
    [ProducesResponseType(typeof(RecordEnvelope<SuiRecord>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(FailureInfo), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetRecord2(
        [FromRoute] string sui,
        [FromRoute] string providerSystemId,
        [FromQuery] string? recordType
    )
    {
        return await ActionResult(sui, recordType, providerSystemId);
    }

    private async Task<IActionResult> ActionResult(
        string sui,
        string? recordType,
        string providerSystemId
    )
    {
        logger.LogInformation("Getting record starting, for sui:'{Sui}'", sui);

        switch (recordType)
        {
            case "education.details":
                var educationDetailsResult = await recordService.GetRecord<EducationDetailsRecord>(
                    sui,
                    providerSystemId
                );
                logger.LogInformation("Getting record ended, for sui:'{Sui}'", sui);
                if (educationDetailsResult != null)
                    return Ok(educationDetailsResult);
                logger.LogInformation("Record not found, for sui:'{Sui}'", sui);
                break;
            case "health.details":
                var healthDetailsResult = await recordService.GetRecord<HealthDataRecord>(
                    sui,
                    providerSystemId
                );
                logger.LogInformation("Getting record ended, for sui:'{Sui}'", sui);
                if (healthDetailsResult != null)
                    return Ok(healthDetailsResult);
                logger.LogInformation("Record not found, for sui:'{Sui}'", sui);
                break;
            case "crime-justice.details":
                var crimeDetailsResult = await recordService.GetRecord<CrimeDataRecord>(
                    sui,
                    providerSystemId
                );
                logger.LogInformation("Getting record ended, for sui:'{Sui}'", sui);
                if (crimeDetailsResult != null)
                    return Ok(crimeDetailsResult);
                logger.LogInformation("Record not found, for sui:'{Sui}'", sui);
                break;
            case "childrens-services.details":
                var childrensServicesDetailsResult =
                    await recordService.GetRecord<ChildrensServicesDetailsRecord>(
                        sui,
                        providerSystemId
                    );
                logger.LogInformation("Getting record ended, for sui:'{Sui}'", sui);
                if (childrensServicesDetailsResult != null)
                    return Ok(childrensServicesDetailsResult);
                logger.LogInformation("Record not found, for sui:'{Sui}'", sui);
                break;
            case "personal.details":
                var personalDetailsResult = await recordService.GetRecord<PersonalDetailsRecord>(
                    sui,
                    providerSystemId
                );
                logger.LogInformation("Getting record ended, for sui:'{Sui}'", sui);
                if (personalDetailsResult != null)
                    return Ok(personalDetailsResult);
                logger.LogInformation("Record not found, for sui:'{Sui}'", sui);
                break;
        }

        return NotFound();
    }
}
