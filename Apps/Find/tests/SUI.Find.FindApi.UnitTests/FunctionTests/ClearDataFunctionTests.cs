using System.Linq.Expressions;
using Azure;
using Azure.Data.Tables;
using Microsoft.Azure.Functions.Worker;
using Microsoft.DurableTask.Client;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using SUI.Find.FindApi.Functions.TimerFunctions;
using SUI.Find.Infrastructure;
using SUI.Find.Infrastructure.Models;

namespace SUI.Find.FindApi.UnitTests.FunctionTests;

// Ignore "Evaluation of this argument may be expensive and unnecessary if logging is disabled" - these are tests!
#pragma warning disable CA1873

public class ClearDataFunctionTests
{
    private readonly ILogger<ClearDataFunction> _mockLogger = Substitute.For<
        ILogger<ClearDataFunction>
    >();
    private readonly IConfiguration _mockConfiguration = Substitute.For<IConfiguration>();
    private readonly ClearDataFunction _function;
    private readonly DurableTaskClient _taskClient = Substitute.For<DurableTaskClient>("test");
    private readonly TableServiceClient _tableServiceClient = Substitute.For<TableServiceClient>();
    private readonly TableClient _tableClient = Substitute.For<TableClient>();
    private readonly TimeProvider _timeProvider = Substitute.For<TimeProvider>();
    private readonly CancellationToken _cancellationToken;

    public ClearDataFunctionTests()
    {
        _cancellationToken = CancellationToken.None;
        _function = new ClearDataFunction(
            _mockLogger,
            _mockConfiguration,
            _timeProvider,
            _tableServiceClient
        );
    }

    [Fact]
    public async Task ClearData_CallsSuccessfully()
    {
        // Arrange
        _mockConfiguration["StorageRetentionDays"].Returns("1");
        _timeProvider.GetUtcNow().Returns(new DateTime(2026, 03, 05));

        var page = Page<FetchUrlMappingEntity>.FromValues(
            [
                new FetchUrlMappingEntity
                {
                    PartitionKey = "9991234567",
                    RowKey = "123456789",
                    Timestamp = new DateTime(2026, 03, 01),
                },
                new FetchUrlMappingEntity
                {
                    PartitionKey = "9991234567",
                    RowKey = "123456789",
                    Timestamp = new DateTime(2026, 03, 05),
                },
            ],
            null,
            Substitute.For<Response>()
        );

        var asyncPageable = AsyncPageable<FetchUrlMappingEntity>.FromPages([page]);

        _tableClient
            .QueryAsync(
                Arg.Any<Expression<Func<FetchUrlMappingEntity, bool>>>(),
                Arg.Any<int?>(),
                Arg.Any<IEnumerable<string>>(),
                Arg.Any<CancellationToken>()
            )
            .Returns(asyncPageable);

        _tableServiceClient
            .GetTableClient(InfrastructureConstants.StorageTableUrlMappings.TableName)
            .Returns(_tableClient);

        var purgeResult = new PurgeResult(1);

        _taskClient
            .PurgeAllInstancesAsync(
                Arg.Any<PurgeInstancesFilter>(),
                null,
                Arg.Any<CancellationToken>()
            )
            .Returns(purgeResult);

        // Act
        await _function.ClearData(new TimerInfo(), _taskClient, _cancellationToken);

        // Assert
        _tableClient
            .Received(1)
            .QueryAsync(
                Arg.Any<Expression<Func<FetchUrlMappingEntity, bool>>>(),
                Arg.Any<int?>(),
                Arg.Any<IEnumerable<string>>(),
                Arg.Any<CancellationToken>()
            );

        await _tableClient
            .Received(1)
            .SubmitTransactionAsync(
                Arg.Any<IEnumerable<TableTransactionAction>>(),
                Arg.Any<CancellationToken>()
            );

        await _taskClient
            .Received(1)
            .PurgeAllInstancesAsync(
                Arg.Any<PurgeInstancesFilter>(),
                Arg.Any<PurgeInstanceOptions>(),
                Arg.Any<CancellationToken>()
            );

        _mockLogger.ReceivedWithAnyArgs(3).LogInformation("*");

        _mockLogger
            .Received()
            .Log(
                LogLevel.Information,
                Arg.Any<EventId>(),
                Arg.Is<Arg.AnyType>(
                    (object x) => $"{x}" == "1 entities deleted from instance history"
                ),
                null,
                Arg.Any<Func<Arg.AnyType, Exception?, string>>()
            );
    }

    [Fact]
    public async Task ClearData_WithNoEntities_DoesNotSubmitTransaction()
    {
        // Arrange
        _mockConfiguration["StorageRetentionDays"].Returns("1");
        _timeProvider.GetUtcNow().Returns(new DateTime(2026, 03, 05));

        _tableServiceClient
            .GetTableClient(InfrastructureConstants.StorageTableUrlMappings.TableName)
            .Returns(_tableClient);

        var purgeResult = new PurgeResult(0);

        _taskClient
            .PurgeAllInstancesAsync(
                Arg.Any<PurgeInstancesFilter>(),
                null,
                Arg.Any<CancellationToken>()
            )
            .Returns(purgeResult);

        // Act
        await _function.ClearData(new TimerInfo(), _taskClient, _cancellationToken);

        // Assert
        _tableClient
            .Received(1)
            .QueryAsync(
                Arg.Any<Expression<Func<FetchUrlMappingEntity, bool>>>(),
                Arg.Any<int?>(),
                Arg.Any<IEnumerable<string>>(),
                Arg.Any<CancellationToken>()
            );

        await _tableClient
            .DidNotReceive()
            .SubmitTransactionAsync(
                Arg.Any<IEnumerable<TableTransactionAction>>(),
                Arg.Any<CancellationToken>()
            );

        await _taskClient
            .Received(1)
            .PurgeAllInstancesAsync(
                Arg.Any<PurgeInstancesFilter>(),
                Arg.Any<PurgeInstanceOptions>(),
                Arg.Any<CancellationToken>()
            );
    }

    [Fact]
    public async Task ClearData_HandlesTableExceptions()
    {
        // Arrange
        _mockConfiguration["StorageRetentionDays"].Returns("1");
        _timeProvider.GetUtcNow().Returns(new DateTime(2026, 03, 05));

        _tableClient
            .QueryAsync(
                Arg.Any<Expression<Func<FetchUrlMappingEntity, bool>>>(),
                Arg.Any<int?>(),
                Arg.Any<IEnumerable<string>>(),
                Arg.Any<CancellationToken>()
            )
            .Throws(new RequestFailedException("Table server failed the request."));

        _tableServiceClient
            .GetTableClient(InfrastructureConstants.StorageTableUrlMappings.TableName)
            .Returns(_tableClient);

        // Act
        await Assert.ThrowsAsync<RequestFailedException>(() =>
            _function.ClearData(new TimerInfo(), _taskClient, _cancellationToken)
        );

        // Assert
        _tableClient
            .Received(1)
            .QueryAsync(
                Arg.Any<Expression<Func<FetchUrlMappingEntity, bool>>>(),
                Arg.Any<int?>(),
                Arg.Any<IEnumerable<string>>(),
                Arg.Any<CancellationToken>()
            );

        _mockLogger
            .Received(1)
            .Log(
                LogLevel.Error,
                Arg.Any<EventId>(),
                Arg.Is<Arg.AnyType>(
                    (object x) =>
                        $"{x}".Contains(
                            "Error querying or clearing table. Table server failed the request."
                        )
                ),
                Arg.Any<RequestFailedException>(),
                Arg.Any<Func<Arg.AnyType, Exception?, string>>()
            );

        await _tableClient
            .DidNotReceive()
            .SubmitTransactionAsync(
                Arg.Any<IEnumerable<TableTransactionAction>>(),
                Arg.Any<CancellationToken>()
            );

        await _taskClient
            .DidNotReceive()
            .PurgeAllInstancesAsync(
                Arg.Any<PurgeInstancesFilter>(),
                Arg.Any<PurgeInstanceOptions>(),
                Arg.Any<CancellationToken>()
            );
    }

    [Fact]
    public async Task ClearData_HandlesTransactionExceptions()
    {
        // Arrange
        _mockConfiguration["StorageRetentionDays"].Returns("1");
        _timeProvider.GetUtcNow().Returns(new DateTime(2026, 03, 05));

        _tableClient
            .QueryAsync(
                Arg.Any<Expression<Func<FetchUrlMappingEntity, bool>>>(),
                Arg.Any<int?>(),
                Arg.Any<IEnumerable<string>>(),
                Arg.Any<CancellationToken>()
            )
            .Throws(
                new TableTransactionFailedException(
                    new RequestFailedException("Table server failed the request.")
                )
            );

        _tableServiceClient
            .GetTableClient(InfrastructureConstants.StorageTableUrlMappings.TableName)
            .Returns(_tableClient);

        // Act
        await Assert.ThrowsAsync<TableTransactionFailedException>(() =>
            _function.ClearData(new TimerInfo(), _taskClient, _cancellationToken)
        );

        // Assert
        _tableClient
            .Received(1)
            .QueryAsync(
                Arg.Any<Expression<Func<FetchUrlMappingEntity, bool>>>(),
                Arg.Any<int?>(),
                Arg.Any<IEnumerable<string>>(),
                Arg.Any<CancellationToken>()
            );

        _mockLogger
            .Received(1)
            .Log(
                LogLevel.Error,
                Arg.Any<EventId>(),
                Arg.Is<Arg.AnyType>(
                    (object x) =>
                        $"{x}".Contains(
                            "One of the batch delete transactions failed. Table server failed the request."
                        )
                ),
                Arg.Any<TableTransactionFailedException>(),
                Arg.Any<Func<Arg.AnyType, Exception?, string>>()
            );

        await _tableClient
            .DidNotReceive()
            .SubmitTransactionAsync(
                Arg.Any<IEnumerable<TableTransactionAction>>(),
                Arg.Any<CancellationToken>()
            );

        await _taskClient
            .DidNotReceive()
            .PurgeAllInstancesAsync(
                Arg.Any<PurgeInstancesFilter>(),
                Arg.Any<PurgeInstanceOptions>(),
                Arg.Any<CancellationToken>()
            );
    }
}
