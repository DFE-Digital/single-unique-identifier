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
    public class FetchController : ControllerBase
    {
        private readonly ILogger<RecordsController> _logger;
        private readonly IMediator _mediator;

        public FetchController(ILogger<RecordsController> logger, IMediator mediator)
        {
            _logger = logger;
            _mediator = mediator;
        }

        [HttpGet("{providerSystemId}/{sui}")]
        [ProducesResponseType(typeof(RecordEnvelope<JsonDocument>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(FailureInfo), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> GetPersonalDetailsRecord(
            [FromRoute] string providerSystemId,
            [FromRoute] string sui
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
    }
}
