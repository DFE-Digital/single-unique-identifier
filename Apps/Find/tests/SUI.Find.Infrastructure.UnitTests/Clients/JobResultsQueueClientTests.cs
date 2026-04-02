using System.Buffers.Text;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using SUI.Find.Application.Enums;
using SUI.Find.Application.Models;
using SUI.Find.Infrastructure.Clients;
using SUI.Find.Infrastructure.Factories;

namespace SUI.Find.Infrastructure.UnitTests.Clients;

public class JobResultsQueueClientTests
{
    private readonly JobResultsQueueClient _client;
    private readonly IQueueClientFactory _queueClientFactory =
        Substitute.For<IQueueClientFactory>();
    private readonly IJobResultsQueueSender _queueSender = Substitute.For<IJobResultsQueueSender>();
    private readonly ILogger<JobResultsQueueClient> _logger = Substitute.For<
        ILogger<JobResultsQueueClient>
    >();

    public JobResultsQueueClientTests()
    {
        _client = new JobResultsQueueClient(_logger, _queueClientFactory);
        _queueClientFactory.GetJobResultsClient().Returns(_queueSender);
    }

    [Fact]
    public async Task SendAsync_ShouldSendBase64EncodedMessageToQueue()
    {
        // Arrange
        _queueSender
            .SendMessageAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        var message = GetMockJobResultMessage();

        // Act
        await _client.SendAsync(message, CancellationToken.None);

        // Assert
        _queueClientFactory.Received(1).GetJobResultsClient();

        await _queueSender
            .Received(1)
            .SendMessageAsync(
                Arg.Is<string>(s => Base64.IsValid(s) && Convert.FromBase64String(s).Length > 1),
                Arg.Any<CancellationToken>()
            );
    }

    [Fact]
    public async Task SendAsync_ShouldLogInformation_WhenMessageSentSuccessfully()
    {
        // Arrange
        var message = GetMockJobResultMessage();

        _queueSender
            .SendMessageAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        // Act
        await _client.SendAsync(message, CancellationToken.None);

        // Assert
        _logger
            .Received(1)
            .Log(
                LogLevel.Information,
                Arg.Any<EventId>(),
                Arg.Is<object>(o => o.ToString()!.Contains("Job results posted to queue")),
                null,
                Arg.Any<Func<object, Exception?, string>>()
            );
    }

    [Fact]
    public async Task SendAsync_ShouldLogError_WhenQueueThrows()
    {
        // Arrange
        _queueSender
            .SendMessageAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Throws(new Exception("Queue send failed"));

        var message = GetMockJobResultMessage();

        // Act
        await _client.SendAsync(message, CancellationToken.None);

        // Assert
        _queueClientFactory.Received(1).GetJobResultsClient();

        await _queueSender
            .Received(1)
            .SendMessageAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());

        _logger
            .Received()
            .Log(
                LogLevel.Error,
                Arg.Any<EventId>(),
                Arg.Any<object>(),
                Arg.Any<Exception>(),
                Arg.Any<Func<object, Exception, string>>()!
            );
    }

    private static JobResultMessage GetMockJobResultMessage() =>
        new()
        {
            JobId = Guid.NewGuid().ToString(),
            WorkItemId = Guid.NewGuid().ToString(),
            LeaseId = Guid.NewGuid().ToString(),
            CustodianId = "test-custodian",
            SubmittedAtUtc = DateTimeOffset.UtcNow,
            JobType = JobType.CustodianLookup,
            JobTraceParent = "00-a79f009d81f57b385d70d5e1202185d8-68e6fb3a378f6fe3-01",
            Records =
            [
                new()
                {
                    RecordId = "rec-1",
                    RecordType = "type-1",
                    RecordUrl = "http://test",
                    SystemId = "sys-1",
                },
            ],
        };
}
