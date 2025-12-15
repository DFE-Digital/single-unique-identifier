using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using SUI.Custodians.Domain.Models;
using SUI.StubCustodians.Application.Common;
using SUI.StubCustodians.Application.Models;
using SUI.StubCustodians.Application.Queries;

namespace SUI.StubCustodians.API.Controllers
{
    [ExcludeFromCodeCoverage]
    [ApiController]
    [Route("api/v{version:apiVersion}/[controller]")]
    [ApiVersion("1.0")]
    public class RecordsController : ControllerBase
    {
        private readonly ILogger<RecordsController> _logger;
        private readonly IMediator _mediator;

        public RecordsController(ILogger<RecordsController> logger, IMediator mediator)
        {
            _logger = logger;
            _mediator = mediator;
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
            _logger.LogInformation("Getting record starting, for sui:'{Sui}'", sui);

            var result = await _mediator.Send(
                new GetPersonalDetailsRecordQuery()
                {
                    Sui = sui,
                    ProviderSystemId = providerSystemId,
                }
            );

            _logger.LogInformation("Getting record ended, for sui:'{Sui}'", sui);

            return result.ToActionResult();
        }

        [HttpGet("{providerSystemId}/ChildSocialCareDetailsRecordV1/{sui}")]
        [ProducesResponseType(
            typeof(RecordEnvelope<ChildSocialCareDetailsRecordV1>),
            StatusCodes.Status200OK
        )]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(FailureInfo), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> GetChildSocialCareDetailsRecord(
            [FromRoute] string sui,
            [FromRoute] string providerSystemId
        )
        {
            _logger.LogInformation("Getting record starting, for sui:'{Sui}'", sui);

            var result = await _mediator.Send(
                new GetChildSocialCareRecordQuery()
                {
                    Sui = sui,
                    ProviderSystemId = providerSystemId,
                }
            );

            _logger.LogInformation("Getting record ended, for sui:'{Sui}'", sui);

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
            _logger.LogInformation("Getting record starting, for sui:'{Sui}'", sui);

            var result = await _mediator.Send(
                new GetEducationDetailsRecordQuery()
                {
                    Sui = sui,
                    ProviderSystemId = providerSystemId,
                }
            );

            _logger.LogInformation("Getting record ended, for sui:'{Sui}'", sui);

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
            _logger.LogInformation("Getting record starting, for sui:'{Sui}'", sui);

            var result = await _mediator.Send(
                new GetHealthDataRecordQuery() { Sui = sui, ProviderSystemId = providerSystemId }
            );

            _logger.LogInformation("Getting record ended, for sui:'{Sui}'", sui);

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
            _logger.LogInformation("Getting record starting, for sui:'{Sui}'", sui);

            var result = await _mediator.Send(
                new GetCrimeDataRecordQuery() { Sui = sui, ProviderSystemId = providerSystemId }
            );

            _logger.LogInformation("Getting record ended, for sui:'{Sui}'", sui);

            return result.ToActionResult();
        }

        [HttpGet("{providerSystemId}/{sui}")]
        [ProducesResponseType(typeof(RecordEnvelope<SuiRecord>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(FailureInfo), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> GetRecord(
            [FromRoute] string sui,
            [FromRoute] string providerSystemId
        )
        {
            _logger.LogInformation("Getting record starting, for sui:'{Sui}'", sui);

            switch (providerSystemId)
            {
                case "1001":
                    var result = await _mediator.Send(
                        new GetPersonalDetailsRecordQuery
                        {
                            Sui = sui,
                            ProviderSystemId = providerSystemId,
                        }
                    );
                    _logger.LogInformation("Getting record ended, for sui:'{Sui}'", sui);
                    return result.ToActionResult();

                case "2001":
                    var result2 = await _mediator.Send(
                        new GetChildSocialCareRecordQuery
                        {
                            Sui = sui,
                            ProviderSystemId = providerSystemId,
                        }
                    );
                    _logger.LogInformation("Getting record ended, for sui:'{Sui}'", sui);
                    return result2.ToActionResult();

                case "3001":
                    var result3 = await _mediator.Send(
                        new GetHealthDataRecordQuery
                        {
                            Sui = sui,
                            ProviderSystemId = providerSystemId,
                        }
                    );
                    _logger.LogInformation("Getting record ended, for sui:'{Sui}'", sui);
                    return result3.ToActionResult();

                case "4001":
                    var result4 = await _mediator.Send(
                        new GetCrimeDataRecordQuery
                        {
                            Sui = sui,
                            ProviderSystemId = providerSystemId,
                        }
                    );
                    _logger.LogInformation("Getting record ended, for sui:'{Sui}'", sui);
                    return result4.ToActionResult();

                case "5001":
                    var result5 = await _mediator.Send(
                        new GetEducationDetailsRecordQuery
                        {
                            Sui = sui,
                            ProviderSystemId = providerSystemId,
                        }
                    );
                    _logger.LogInformation("Getting record ended, for sui:'{Sui}'", sui);
                    return result5.ToActionResult();
            }

            return NotFound();
        }
    }
}
