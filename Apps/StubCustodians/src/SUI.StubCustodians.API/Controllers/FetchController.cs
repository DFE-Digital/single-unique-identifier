using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using SUI.Custodians.Domain.Models;
using SUI.StubCustodians.Application.Common;
using SUI.StubCustodians.Application.Models;
using SUI.StubCustodians.Application.Queries;

namespace SUI.StubCustodians.API.Controllers;

[ExcludeFromCodeCoverage]
[ApiController]
[Route("api/v{version:apiVersion}/[controller]")]
[ApiVersion("1.0")]
public class FetchController(ILogger<RecordsController> logger, IMediator mediator) : ControllerBase
{
    [HttpGet("{providerSystemId}/{recordType}/{sui}")]
    [ProducesResponseType(typeof(RecordEnvelope<SuiRecord>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(FailureInfo), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetRecord(
        [FromRoute] string sui,
        [FromRoute] string recordType,
        [FromRoute] string providerSystemId
    )
    {
        logger.LogInformation("Getting record starting, for sui:'{Sui}'", sui);

        switch (recordType)
        {
            case "PersonalDetailsRecordV1":
                var result = await mediator.Send(
                    new GetPersonalDetailsRecordQuery
                    {
                        Sui = sui,
                        ProviderSystemId = providerSystemId,
                    }
                );
                logger.LogInformation("Getting record ended, for sui:'{Sui}'", sui);
                return result.ToActionResult();

            case "ChildrensServicesDetailsRecordV1":
                var result2 = await mediator.Send(
                    new GetChildrensServicesDetailsRecordQuery
                    {
                        Sui = sui,
                        ProviderSystemId = providerSystemId,
                    }
                );
                logger.LogInformation("Getting record ended, for sui:'{Sui}'", sui);
                return result2.ToActionResult();

            case "HealthDataRecordV1":
                var result3 = await mediator.Send(
                    new GetHealthDataRecordQuery { Sui = sui, ProviderSystemId = providerSystemId }
                );
                logger.LogInformation("Getting record ended, for sui:'{Sui}'", sui);
                return result3.ToActionResult();

            case "CrimeDataRecordV1":
                var result4 = await mediator.Send(
                    new GetCrimeDataRecordQuery { Sui = sui, ProviderSystemId = providerSystemId }
                );
                logger.LogInformation("Getting record ended, for sui:'{Sui}'", sui);
                return result4.ToActionResult();

            case "EducationDetailsRecordV1":
                var result5 = await mediator.Send(
                    new GetEducationDetailsRecordQuery
                    {
                        Sui = sui,
                        ProviderSystemId = providerSystemId,
                    }
                );
                logger.LogInformation("Getting record ended, for sui:'{Sui}'", sui);
                return result5.ToActionResult();
        }

        return NotFound();
    }
}
