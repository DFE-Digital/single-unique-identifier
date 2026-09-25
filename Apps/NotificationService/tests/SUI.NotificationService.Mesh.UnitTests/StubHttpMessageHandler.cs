using System.Net;

namespace SUI.NotificationService.Mesh.UnitTests;

/// <summary>
/// Records every request and answers from a queue of canned responses, in order.
/// </summary>
internal sealed class StubHttpMessageHandler : HttpMessageHandler
{
    private readonly Queue<HttpResponseMessage> _responses = new();

    public List<HttpRequestMessage> Requests { get; } = [];

    public StubHttpMessageHandler Respond(string json)
    {
        _responses.Enqueue(
            new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(json) }
        );
        return this;
    }

    public StubHttpMessageHandler Respond(HttpStatusCode statusCode)
    {
        _responses.Enqueue(new HttpResponseMessage(statusCode));
        return this;
    }

    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken
    )
    {
        Requests.Add(request);
        return Task.FromResult(_responses.Dequeue());
    }

    // Dequeued responses belong to the caller; only those a test queued but never consumed are ours.
    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            while (_responses.TryDequeue(out var response))
            {
                response.Dispose();
            }
        }

        base.Dispose(disposing);
    }
}
