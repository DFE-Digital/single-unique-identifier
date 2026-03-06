using Azure.Data.Tables;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using SUI.Find.Infrastructure.Enums;
using SUI.Find.Infrastructure.Repositories.WorkItemJobCountRepository;

namespace SUI.Find.Infrastructure.UnitTests.Repositories;

public class WorkItemJobCountRepositoryExceptionTests
{
    [Fact]
    public async Task UpsertAsync_WhenTableThrows_LogsError()
    {
        // Arrange
        var client = Substitute.For<TableServiceClient>();
        var tableClient = Substitute.For<TableClient>();

        tableClient
            .UpsertEntityAsync(
                Arg.Any<TableEntity>(),
                Arg.Any<TableUpdateMode>(),
                Arg.Any<CancellationToken>()
            )
            .Throws(new Exception("Boom"));

        client.GetTableClient(Arg.Any<string>()).Returns(tableClient);

        var logger = Substitute.For<ILogger<WorkItemJobCountRepository>>();
        var repo = new WorkItemJobCountRepository(client, logger);

        var entity = new WorkItemJobCount
        {
            WorkItemId = "WI-1",
            JobType = JobType.CustodianLookup,
            ExpectedJobCount = 1,
            CreatedAtUtc = DateTimeOffset.UtcNow,
            UpdatedAtUtc = DateTimeOffset.UtcNow,
            PayloadJson = "{}",
        };

        // Act
        await repo.UpsertAsync(entity);

        // Assert
        logger
            .Received(1)
            .Log(
                LogLevel.Error,
                Arg.Any<EventId>(),
                Arg.Any<Arg.AnyType>(),
                Arg.Is<Exception>(ex => ex.Message == "Boom"),
                Arg.Any<Func<Arg.AnyType, Exception?, string>>()
            );
    }

    [Fact]
    public async Task GetByWorkItemIdAndJobTypeAsync_WhenTableThrows_LogsWarningAndReturnsNull()
    {
        // Arrange
        var client = Substitute.For<TableServiceClient>();
        var tableClient = Substitute.For<TableClient>();

        // Setup the throw
        tableClient
            .GetEntityIfExistsAsync<TableEntity>(
                Arg.Any<string>(),
                Arg.Any<string>(),
                null,
                Arg.Any<CancellationToken>()
            )
            .Throws(new Exception("Read failed"));

        client.GetTableClient(Arg.Any<string>()).Returns(tableClient);

        var logger = Substitute.For<ILogger<WorkItemJobCountRepository>>();
        var repo = new WorkItemJobCountRepository(client, logger);

        // Act
        var result = await repo.GetByWorkItemIdAndJobTypeAsync("WI-1", JobType.CustodianLookup);

        // Assert
        Assert.Null(result);

        logger
            .Received(1)
            .Log(
                LogLevel.Warning,
                Arg.Any<EventId>(),
                Arg.Any<Arg.AnyType>(),
                Arg.Is<Exception>(ex => ex.Message == "Read failed"),
                Arg.Any<Func<Arg.AnyType, Exception?, string>>()
            );
    }
}
