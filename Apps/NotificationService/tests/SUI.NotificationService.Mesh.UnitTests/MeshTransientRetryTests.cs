using System.Net;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SUI.NotificationService.Application.Interfaces;

namespace SUI.NotificationService.Mesh.UnitTests;

public sealed class MeshTransientRetryTests
{
    [Fact]
    public async Task ReadMessageAsync_WhenMeshReturnsTransientError_RetriesAndSucceeds()
    {
        using var handler = new StubHttpMessageHandler()
            .Respond(HttpStatusCode.ServiceUnavailable)
            .Respond(HttpStatusCode.ServiceUnavailable)
            .Respond("message-body");
        await using var provider = BuildProvider(handler);

        var message = await provider
            .GetRequiredService<IMeshInboxClient>()
            .ReadMessageAsync("message-1");

        Assert.Equal("message-body", message.Content);
        Assert.Equal(3, handler.Requests.Count);
    }

    [Fact]
    public async Task ReadMessageAsync_WhenMeshReturnsNotFound_DoesNotRetry()
    {
        using var handler = new StubHttpMessageHandler().Respond(HttpStatusCode.NotFound);
        await using var provider = BuildProvider(handler);

        await Assert.ThrowsAsync<HttpRequestException>(() =>
            provider.GetRequiredService<IMeshInboxClient>().ReadMessageAsync("message-1")
        );

        Assert.Single(handler.Requests);
    }

    private static ServiceProvider BuildProvider(HttpMessageHandler primaryHandler)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(
                new Dictionary<string, string?>
                {
                    ["NhsMeshConfig:MailboxBaseUrl"] = "https://localhost:8700",
                    ["NhsMeshConfig:SharedKey"] = "test-shared-key",
                    ["NhsMeshConfig:MailboxId"] = "X26ABC1",
                    ["NhsMeshConfig:MailboxPassword"] = "test-password",
                }
            )
            .Build();

        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(configuration);
        services.AddMeshIntegration();
        services
            .AddHttpClient(MeshInboxClient.HttpClientName)
            .ConfigurePrimaryHttpMessageHandler(() => primaryHandler);

        return services.BuildServiceProvider();
    }
}
