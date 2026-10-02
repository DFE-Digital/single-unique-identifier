using NSubstitute;

namespace SUI.NotificationService.Application.UnitTests.Services.MeshMessageProcessorTests;

public sealed class AcknowledgeMessageAsyncTests : MeshMessageProcessorTestBase
{
    [Fact]
    public async Task AcknowledgeMessageAsync_AcknowledgesTheMessageOnTheMailbox()
    {
        using var cancellation = new CancellationTokenSource();

        await CreateProcessor().AcknowledgeMessageAsync("message-1", cancellation.Token);

        await MeshInboxClient.Received(1).AcknowledgeMessageAsync("message-1", cancellation.Token);
    }
}
