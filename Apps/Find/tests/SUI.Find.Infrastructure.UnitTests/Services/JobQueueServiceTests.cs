using System;
using System.Buffers.Text;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using SUI.Find.Application.Enums;
using SUI.Find.Application.Interfaces;
using SUI.Find.Infrastructure.Clients;
using SUI.Find.Infrastructure.Factories;
using SUI.Find.Infrastructure.Models;
using SUI.Find.Infrastructure.Services;
using Xunit;

namespace SUI.Find.Infrastructure.UnitTests.Services;

public class JobQueueServiceTests
{
    private readonly JobQueueService _jobQueueService;
    private readonly ILogger<JobQueueService> _logger = Substitute.For<ILogger<JobQueueService>>();
    private readonly IQueueClientFactory _queueClientFactory =
        Substitute.For<IQueueClientFactory>();
    private readonly ISearchJobQueueSender _searchJobQueueSender =
        Substitute.For<ISearchJobQueueSender>();

    public JobQueueServiceTests()
    {
        _queueClientFactory.GetSearchJobClient().Returns(_searchJobQueueSender);
        _jobQueueService = new JobQueueService(_logger, _queueClientFactory);
    }

    [Fact]
    public async Task PostSearchJobAsync_ShouldSendMessageToQueue_AndReturnJobDto()
    {
        // Arrange
        var requestMessage = new SearchRequestMessage
        {
            WorkItemId = Guid.NewGuid(),
            PersonId = "test-person",
            RequestingCustodianId = "test-custodian",
            TraceId = "trace-123",
            InvocationId = "inv-456",
        };

        _searchJobQueueSender
            .SendMessageAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        // Act
        var result = await _jobQueueService.PostSearchJobAsync(
            requestMessage,
            CancellationToken.None
        );

        // Assert
        _queueClientFactory.Received(1).GetSearchJobClient();

        await _searchJobQueueSender
            .Received(1)
            .SendMessageAsync(
                Arg.Is<string>(s => IsValidBase64Message(s, requestMessage)),
                Arg.Any<CancellationToken>()
            );

        Assert.NotNull(result);
        Assert.Equal(requestMessage.WorkItemId.ToString(), result.JobId);
        Assert.Equal(requestMessage.PersonId, result.PersonId);
        Assert.Equal(SearchStatus.Queued, result.Status);
    }

    [Fact]
    public async Task PostSearchJobAsync_ShouldLogErrorOnException_AndRethrow()
    {
        // Arrange
        var requestMessage = new SearchRequestMessage
        {
            WorkItemId = Guid.NewGuid(),
            PersonId = "test-person",
            RequestingCustodianId = "test-custodian",
            TraceId = "trace-123",
            InvocationId = "inv-456",
        };

        var exception = new Exception("Queue send failed");

        _searchJobQueueSender
            .SendMessageAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Throws(exception);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<Exception>(() =>
            _jobQueueService.PostSearchJobAsync(requestMessage, CancellationToken.None)
        );

        Assert.Equal("Queue send failed", ex.Message);

        _queueClientFactory.Received(1).GetSearchJobClient();
        await _searchJobQueueSender
            .Received(1)
            .SendMessageAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());

        _logger
            .Received()
            .Log(
                LogLevel.Error,
                Arg.Any<EventId>(),
                Arg.Any<object>(),
                exception,
                Arg.Any<Func<object, Exception?, string>>()
            );
    }

    private static bool IsValidBase64Message(
        string base64String,
        SearchRequestMessage expectedMessage
    )
    {
        try
        {
            var bytes = Convert.FromBase64String(base64String);
            var json = Encoding.UTF8.GetString(bytes);
            var deserialized = JsonSerializer.Deserialize<SearchRequestMessage>(json);

            return deserialized != null
                && deserialized.WorkItemId == expectedMessage.WorkItemId
                && deserialized.PersonId == expectedMessage.PersonId
                && deserialized.RequestingCustodianId == expectedMessage.RequestingCustodianId;
        }
        catch
        {
            return false;
        }
    }
}
