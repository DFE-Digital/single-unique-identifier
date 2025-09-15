using System.Net;
using System.Text;
using System.Text.Json;

namespace SUI.Find.Infrastructure.UnitTests.TestUtils;

public class MockHttpMessageHandler : HttpMessageHandler
{
    public int NumberOfCalls { get; private set; }
    
    public int ExpiresInSeconds { get; set; } = 300;

    public HttpStatusCode StatusCode { get; set; } = HttpStatusCode.OK;

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        NumberOfCalls++;
        var tokenResponse = new { access_token = "a.dummy.token", expires_in = ExpiresInSeconds };
        var response = new HttpResponseMessage
        {
            StatusCode = StatusCode,
            Content = new StringContent(JsonSerializer.Serialize(tokenResponse), Encoding.UTF8, "application/json")
        };
        return Task.FromResult(response);
    }
}