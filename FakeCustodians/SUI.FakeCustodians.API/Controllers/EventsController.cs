using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;

namespace SUI.FakeCustodians.API.Controllers
{
    [ApiController]
    [Route("api/v{version:apiVersion}/[controller]")]
    [ApiVersion("1.0")]
    public class EventsController : ControllerBase
    {
        // GET: api/<EventsController>
        [HttpGet("{sui}")]
        public IEnumerable<string> GetEventsBySui(string sui)
        {
            return [$"{sui}"];
        }
    }
}
