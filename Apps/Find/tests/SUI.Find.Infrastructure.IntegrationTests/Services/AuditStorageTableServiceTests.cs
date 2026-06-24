using System.Text.Json;
using Azure.Data.Tables;
using SUI.Find.Domain.Events.Audit;
using SUI.Find.Infrastructure.Services;

namespace SUI.Find.Infrastructure.IntegrationTests.Services;

public class AuditStorageTableServiceTests : IAsyncLifetime
{
    private readonly AuditStorageTableService _sut = new(TableStorageFixture.Client);

    public async Task InitializeAsync() =>
        await _sut.EnsureTableExistsAsync(CancellationToken.None);

    public Task DisposeAsync() => Task.CompletedTask;

    [Theory]
    [InlineData("00-0af7651916cd43dd8448eb211c80319c-b7ad6b7169203331-01")]
    [InlineData(null)]
    [InlineData("")]
    public async Task WriteAccessAuditLogAsync_CreatesRecord_AsExpected(string? traceParent)
    {
        var inputTimestamp = new DateTime(2026, 2, 16, 12, 23, 45, 123, 456, DateTimeKind.Utc);
        const string inputPayload = """{"test": true}""";
        var input = new AuditEvent
        {
            EventId = $"EventId_{Guid.NewGuid()}",
            EventName = $"EventName_{Guid.NewGuid()}",
            ServiceName = $"ServiceName_{Guid.NewGuid()}",
            Actor = new AuditActor
            {
                ActorId = $"ActorId_{Guid.NewGuid()}",
                ActorRole = $"ActorRole_{Guid.NewGuid()}",
            },
            Payload = JsonElement.Parse(inputPayload),
            Timestamp = inputTimestamp,
            CorrelationId = $"CorrelationId_{Guid.NewGuid()}",
            TraceParent = traceParent,
        };

        var expectedPartitionKey = $"{inputTimestamp:yyyy-MM-dd}";
        var expectedRowKey = input.EventId;

        // ACT
        await _sut.WriteAccessAuditLogAsync(input, CancellationToken.None);

        // ASSERT
        var entity = (
            await TableStorageFixture
                .Client.GetTableClient(InfrastructureConstants.StorageTableAudit.TableName)
                .GetEntityAsync<TableEntity>(expectedPartitionKey, expectedRowKey)
        ).Value;

        var result = new AuditEvent
        {
            EventId = entity.GetString("EventId"),
            EventName = entity.GetString("EventName"),
            ServiceName = entity.GetString("ServiceName"),
            Actor = new AuditActor
            {
                ActorId = entity.GetString("ActorId"),
                ActorRole = entity.GetString("ActorRole"),
            },
            Payload = JsonElement.Parse(entity.GetString("Payload")),
            Timestamp = inputTimestamp,
            CorrelationId = entity.GetString("CorrelationId"),
            TraceParent = entity.GetString("TraceParent"),
        };

        result.Should().NotBeSameAs(input);
        result.Should().BeEquivalentTo(input, opts => opts.Excluding(x => x.Payload));

        result.Payload.ToString().Should().Be(inputPayload);
    }
}
