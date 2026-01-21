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
public class FetchController : ControllerBase
{
    private readonly ILogger<FetchController> _logger;
    private readonly IRecordServiceHandler<PersonalDetailsRecordV1> _personalDetailsRecordHandler;
    private readonly IRecordServiceHandler<ChildrensServicesDetailsRecordV1> _childrensServicesDetailsRecordHandler;
    private readonly IRecordServiceHandler<HealthDataRecordV1> _healthDataRecordHandler;
    private readonly IRecordServiceHandler<EducationDetailsRecordV1> _educationDetailsRecordHandler;
    private readonly IRecordServiceHandler<CrimeDataRecordV1> _crimeDataRecordHandler;

    public FetchController(
        ILogger<FetchController> logger,
        IRecordServiceHandler<PersonalDetailsRecordV1> personalDetailsRecordHandler,
        IRecordServiceHandler<ChildrensServicesDetailsRecordV1> childrensServicesDetailsRecordHandler,
        IRecordServiceHandler<HealthDataRecordV1> healthDataRecordHandler,
        IRecordServiceHandler<EducationDetailsRecordV1> educationDetailsRecordHandler,
        IRecordServiceHandler<CrimeDataRecordV1> crimeDataRecordHandler
    )
    {
        _logger = logger;
        _personalDetailsRecordHandler = personalDetailsRecordHandler;
        _childrensServicesDetailsRecordHandler = childrensServicesDetailsRecordHandler;
        _healthDataRecordHandler = healthDataRecordHandler;
        _educationDetailsRecordHandler = educationDetailsRecordHandler;
        _crimeDataRecordHandler = crimeDataRecordHandler;
    }

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
        _logger.LogInformation("Getting record starting, for sui:'{Sui}'", sui);

        switch (recordType)
        {
            case nameof(PersonalDetailsRecord):
                var result = await _personalDetailsRecordHandler.GetRecord(sui, providerSystemId);
                _logger.LogInformation("Getting record ended, for sui:'{Sui}'", sui);
                return result.ToActionResult();

            case nameof(ChildrensServicesDetailsRecord):
                var result2 = await _childrensServicesDetailsRecordHandler.GetRecord(
                    sui,
                    providerSystemId
                );
                _logger.LogInformation("Getting record ended, for sui:'{Sui}'", sui);
                return result2.ToActionResult();

            case nameof(HealthDataRecord):
                var result3 = await _healthDataRecordHandler.GetRecord(sui, providerSystemId);
                _logger.LogInformation("Getting record ended, for sui:'{Sui}'", sui);
                return result3.ToActionResult();

            case nameof(CrimeDataRecord):
                var result4 = await _crimeDataRecordHandler.GetRecord(sui, providerSystemId);
                _logger.LogInformation("Getting record ended, for sui:'{Sui}'", sui);
                return result4.ToActionResult();

            case nameof(EducationDetailsRecord):
                var result5 = await _educationDetailsRecordHandler.GetRecord(sui, providerSystemId);
                _logger.LogInformation("Getting record ended, for sui:'{Sui}'", sui);
                return result5.ToActionResult();
        }

        return NotFound();
    }
}
