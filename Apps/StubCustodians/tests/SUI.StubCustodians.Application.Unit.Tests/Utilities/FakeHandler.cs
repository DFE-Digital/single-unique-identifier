namespace SUI.StubCustodians.Application.Unit.Tests.Utilities;

internal class FakeHandler : HttpMessageHandler
{
    private readonly Func<HttpRequestMessage, HttpResponseMessage> _handler;

    public int CallCount { get; private set; }

    public FakeHandler(Func<HttpRequestMessage, HttpResponseMessage> handler)
    {
        _handler = handler;
    }

    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken
    )
    {
        CallCount++;
        return Task.FromResult(_handler(request));
    }
}
