using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using SUI.FakeCustodians.Application.Common;
using SUI.FakeCustodians.Application.Models;
using SUI.FakeCustodians.Application.Queries;

namespace SUI.FakeCustodians.API.Controllers
{
    [ApiController]
    [Route("api/v{version:apiVersion}/[controller]")]
    [ApiVersion("1.0")]
    public class EventsController : ControllerBase
    {
        private readonly ILogger<EventsController> _logger;
        private readonly IMediator _mediator;

        public EventsController(ILogger<EventsController> logger, IMediator mediator)
        {
            _logger = logger;
            _mediator = mediator;
        }

        [HttpGet("{sui}")]
        [ProducesResponseType(typeof(EventResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(FailureInfo), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> GetEventsBySui([FromRoute] string sui)
        {
            _logger.LogInformation("Getting event record starting, for sui:'{Sui}'", sui);

            var result = await _mediator.Send(new GetEventRecordBySuiQuery() { Sui = sui });

            _logger.LogInformation("Getting event record ended, for sui:'{Sui}'", sui);

            return result.ToActionResult();
        }
    }
}
