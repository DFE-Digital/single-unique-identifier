using Microsoft.AspNetCore.Mvc;

namespace SUI.FakeCustodians.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class EventsController : ControllerBase
    {
        // GET: api/<EventsController>
        [HttpGet]
        public IEnumerable<string> Get()
        {
            return ["value1", "value2"];
        }
    }
}
