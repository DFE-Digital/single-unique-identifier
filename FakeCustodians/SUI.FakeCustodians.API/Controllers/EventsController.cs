using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.Mvc;
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
        public async Task<IActionResult> GetEventsBySui([FromRoute] string sui)
        {
            var result = await _mediator.Send(new GetEventRecordBySuiQuery() { Sui = sui });

            return result.ToActionResult();
        }
    }
}
