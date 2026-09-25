using System.Net;
using Microsoft.Extensions.Options;
using SUI.NotificationService.Mesh.Configuration;

namespace SUI.NotificationService.Mesh.UnitTests;

public sealed class MeshInboxClientTests : IDisposable
{
    private const string MailboxId = "X26ABC1";
    private const string InboxUri = $"/messageexchange/{MailboxId}/inbox";

    private readonly StubHttpMessageHandler _handler = new();
    private readonly HttpClient _httpClient;

    public MeshInboxClientTests()
    {
        _httpClient = new HttpClient(_handler) { BaseAddress = new Uri("https://localhost:8700") };
    }

    // Disposing the HttpClient also disposes the handler it owns.
    public void Dispose() => _httpClient.Dispose();

    [Fact]
    public async Task GetMessageIdsAsync_RequestsTheV2InboxRepresentation()
    {
        _handler.Respond("""{ "messages": [] }""");

        await CreateClient().GetMessageIdsAsync();

        var request = Assert.Single(_handler.Requests);
        Assert.Equal(InboxUri, request.RequestUri!.AbsolutePath);
        Assert.Contains(
            request.Headers.Accept,
            accept => accept.MediaType == "application/vnd.mesh.v2+json"
        );
    }

    [Fact]
    public async Task GetMessageIdsAsync_ReturnsMessageIds_FromASinglePage()
    {
        _handler.Respond("""{ "messages": ["message-1", "message-2"] }""");

        var messageIds = await CreateClient().GetMessageIdsAsync();

        Assert.Equal(["message-1", "message-2"], messageIds);
    }

    [Fact]
    public async Task GetMessageIdsAsync_FollowsNextLinks_UntilNoFurtherPageIsOffered()
    {
        _handler
            .Respond(
                $$"""{ "messages": ["message-1"], "links": { "next": "{{InboxUri}}?continue_from=a" } }"""
            )
            .Respond(
                $$"""{ "messages": ["message-2"], "links": { "next": "{{InboxUri}}?continue_from=b" } }"""
            )
            .Respond("""{ "messages": ["message-3"] }""");

        var messageIds = await CreateClient().GetMessageIdsAsync();

        Assert.Equal(["message-1", "message-2", "message-3"], messageIds);
        Assert.Equal(3, _handler.Requests.Count);
        Assert.Equal("?continue_from=b", _handler.Requests[2].RequestUri!.Query);
    }

    [Fact]
    public async Task GetMessageIdsAsync_Throws_WhenPagingRevisitsAPage()
    {
        _handler
            .Respond(
                $$"""{ "messages": ["message-1"], "links": { "next": "{{InboxUri}}?continue_from=a" } }"""
            )
            .Respond(
                $$"""{ "messages": ["message-2"], "links": { "next": "{{InboxUri}}?continue_from=a" } }"""
            );

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            CreateClient().GetMessageIdsAsync()
        );
    }

    [Fact]
    public async Task GetMessageIdsAsync_Throws_WhenMeshReturnsAnError()
    {
        _handler.Respond(HttpStatusCode.Forbidden);

        await Assert.ThrowsAsync<HttpRequestException>(() => CreateClient().GetMessageIdsAsync());
    }

    [Fact]
    public async Task ReadMessageAsync_ReturnsTheMessageBody()
    {
        _handler.Respond("""{ "resourceType": "Bundle" }""");

        var message = await CreateClient().ReadMessageAsync("message-1");

        Assert.Equal("message-1", message.MessageId);
        Assert.Equal("""{ "resourceType": "Bundle" }""", message.Content);
        Assert.Equal($"{InboxUri}/message-1", _handler.Requests[0].RequestUri!.AbsolutePath);
    }

    [Fact]
    public async Task AcknowledgeMessageAsync_PutsTheAcknowledgedStatus()
    {
        _handler.Respond(HttpStatusCode.OK);

        await CreateClient().AcknowledgeMessageAsync("message-1");

        var request = Assert.Single(_handler.Requests);
        Assert.Equal(HttpMethod.Put, request.Method);
        Assert.Equal($"{InboxUri}/message-1/status/acknowledged", request.RequestUri!.AbsolutePath);
    }

    [Fact]
    public async Task AcknowledgeMessageAsync_Throws_WhenMeshReturnsAnError()
    {
        _handler.Respond(HttpStatusCode.NotFound);

        await Assert.ThrowsAsync<HttpRequestException>(() =>
            CreateClient().AcknowledgeMessageAsync("message-1")
        );
    }

    private MeshInboxClient CreateClient() =>
        new(
            _httpClient,
            Options.Create(
                new NhsMeshConfig
                {
                    MailboxBaseUrl = "https://localhost:8700",
                    MailboxId = MailboxId,
                    MailboxPassword = "password",
                    SharedKey = "shared-key",
                }
            )
        );
}
