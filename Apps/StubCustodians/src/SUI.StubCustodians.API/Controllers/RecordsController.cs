using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;
using SUI.Custodians.Domain.Models;
using SUI.StubCustodians.Application.Common;
using SUI.StubCustodians.Application.Interfaces;
using SUI.StubCustodians.Application.Models;

namespace SUI.StubCustodians.API.Controllers
{
    [ApiController]
    [Route("api/v{version:apiVersion}/[controller]")]
    [ApiVersion("1.0")]
    public class RecordsController : ControllerBase
    {
        private readonly ILogger<RecordsController> _logger;
        private readonly IRecordServiceHandler<PersonalDetailsRecordV1> _personalDetailsRecordHandler;
        private readonly IRecordServiceHandler<ChildrensServicesDetailsRecordV1> _childrensServicesDetailsRecordHandler;
        private readonly IRecordServiceHandler<HealthDataRecordV1> _healthDataRecordHandler;
        private readonly IRecordServiceHandler<EducationDetailsRecordV1> _educationDetailsRecordHandler;
        private readonly IRecordServiceHandler<CrimeDataRecordV1> _crimeDataRecordHandler;

        private const string LogStartMessage = "Getting record starting, for sui:'{Sui}'";
        private const string LogEndMessage = "Getting record ended, for sui:'{Sui}'";

        public RecordsController(
            ILogger<RecordsController> logger,
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

        [HttpGet("{providerSystemId}/PersonalDetailsRecordV1/{sui}")]
        [ProducesResponseType(
            typeof(RecordEnvelope<PersonalDetailsRecordV1>),
            StatusCodes.Status200OK
        )]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(FailureInfo), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> GetPersonalDetailsRecord(
            [FromRoute] string sui,
            [FromRoute] string providerSystemId
        )
        {
            _logger.LogInformation(LogStartMessage, sui);

            var result = await _personalDetailsRecordHandler.GetRecord(sui, providerSystemId);

            _logger.LogInformation(LogEndMessage, sui);

            return result.ToActionResult();
        }

        [HttpGet("{providerSystemId}/ChildrensServicesDetailsRecordV1/{sui}")]
        [ProducesResponseType(
            typeof(RecordEnvelope<ChildrensServicesDetailsRecordV1>),
            StatusCodes.Status200OK
        )]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(FailureInfo), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> GetChildrensServicesDetailsRecord(
            [FromRoute] string sui,
            [FromRoute] string providerSystemId
        )
        {
            _logger.LogInformation(LogStartMessage, sui);

            var result = await _childrensServicesDetailsRecordHandler.GetRecord(
                sui,
                providerSystemId
            );

            _logger.LogInformation(LogEndMessage, sui);

            return result.ToActionResult();
        }

        [HttpGet("{providerSystemId}/EducationDetailsRecordV1/{sui}")]
        [ProducesResponseType(
            typeof(RecordEnvelope<EducationDetailsRecordV1>),
            StatusCodes.Status200OK
        )]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(FailureInfo), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> GetEducationDetailsRecord(
            [FromRoute] string sui,
            [FromRoute] string providerSystemId
        )
        {
            _logger.LogInformation(LogStartMessage, sui);

            var result = await _educationDetailsRecordHandler.GetRecord(sui, providerSystemId);

            _logger.LogInformation(LogEndMessage, sui);

            return result.ToActionResult();
        }

        [HttpGet("{providerSystemId}/HealthDataRecordV1/{sui}")]
        [ProducesResponseType(typeof(RecordEnvelope<HealthDataRecordV1>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(FailureInfo), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> GetHealthDataRecord(
            [FromRoute] string sui,
            [FromRoute] string providerSystemId
        )
        {
            _logger.LogInformation(LogStartMessage, sui);

            var result = await _healthDataRecordHandler.GetRecord(sui, providerSystemId);

            _logger.LogInformation(LogEndMessage, sui);

            return result.ToActionResult();
        }

        [HttpGet("{providerSystemId}/CrimeDataRecordV1/{sui}")]
        [ProducesResponseType(typeof(RecordEnvelope<CrimeDataRecordV1>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(FailureInfo), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> GetCrimeDataRecord(
            [FromRoute] string sui,
            [FromRoute] string providerSystemId
        )
        {
            _logger.LogInformation(LogStartMessage, sui);

            var result = await _crimeDataRecordHandler.GetRecord(sui, providerSystemId);

            _logger.LogInformation(LogEndMessage, sui);

            return result.ToActionResult();
        }
    }
}
