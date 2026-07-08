using System.Net;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;

namespace SUI.GetAnIdentifier.Function.Functions;

public class HelloWorldFunction
{
    [Function(nameof(HelloWorld))]
    public async Task<HttpResponseData> HelloWorld(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "hello-world")]
            HttpRequestData req
    )
    {
        var response = req.CreateResponse(HttpStatusCode.OK);
        await response.WriteAsJsonAsync("Hello World");

        return response;
    }
}
