using Azure.Data.Tables;
using Microsoft.Extensions.Logging.Abstractions;
using SUI.Find.Infrastructure.Repositories.SuiCustodianRegister;

namespace SUI.Find.Infrastructure.IntegrationTests.Repositories;

public class SuiCustodianRegisterRepositoryTests : IAsyncLifetime
{
    private readonly SuiCustodianRegisterRepository _sut = new(
        TableStorageFixture.Client,
        NullLogger<SuiCustodianRegisterRepository>.Instance
    );

    public async Task InitializeAsync() =>
        await _sut.EnsureTableExistsAsync(CancellationToken.None);

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task UpsertAsync_CreatesNewEntry_AsExpected()
    {
        // ARRANGE
        var input = new IdRegisterEntry
        {
            Sui = $"Sui_{Guid.NewGuid()}",
            CustodianId = $"Custodian_{Guid.NewGuid()}",
            SystemId = $"System_{Guid.NewGuid()}",
            RecordType = $"RecordType_{Guid.NewGuid()}",
            CustodianSubjectId = $"Subject_{Guid.NewGuid()}",
            Provenance = Provenance.IssuedByService,
            LastIdDeliveredAtUtc = DateTimeOffset.UtcNow,
        };

        var expectedPartitionKey = RegisterKeys.PartitionKey(input.Sui);
        var expectedRowKey = RegisterKeys.RowKey(
            input.CustodianId,
            input.RecordType,
            input.SystemId
        );

        // ACT
        await _sut.UpsertAsync(input, CancellationToken.None);

        // ASSERT
        var entity = (
            await TableStorageFixture
                .Client.GetTableClient(InfrastructureConstants.StorageTableIdRegister.TableName)
                .GetEntityAsync<TableEntity>(expectedPartitionKey, expectedRowKey)
        ).Value;

        entity.GetString("Sui").Should().Be(input.Sui);
        entity.GetString("CustodianId").Should().Be(input.CustodianId);
        entity.GetString("RecordType").Should().Be(input.RecordType);
        entity.GetString("SystemId").Should().Be(input.SystemId);
        entity.GetString("CustodianSubjectId").Should().Be(input.CustodianSubjectId);
        entity.GetString("Provenance").Should().Be(input.Provenance.ToString());

        entity.GetDateTimeOffset("FirstSeenUtc").Should().NotBeNull();
        entity.GetDateTimeOffset("LastSeenUtc").Should().NotBeNull();
        entity.GetDateTimeOffset("LastIdDeliveredUtc").Should().NotBeNull();
    }

    [Fact]
    public async Task UpsertAsync_DiscoveredViaFanout_RemovesLastIdDeliveredUtc()
    {
        // ARRANGE
        var sui = $"Sui_{Guid.NewGuid()}";
        var custodianId = $"Custodian_{Guid.NewGuid()}";
        var systemId = $"System_{Guid.NewGuid()}";
        var recordType = $"RecordType_{Guid.NewGuid()}";

        // First write with issuance
        await _sut.UpsertAsync(
            new IdRegisterEntry
            {
                Sui = sui,
                CustodianId = custodianId,
                SystemId = systemId,
                RecordType = recordType,
                Provenance = Provenance.IssuedByService,
                LastIdDeliveredAtUtc = DateTimeOffset.UtcNow,
            },
            CancellationToken.None
        );

        // Second write via fanout (should remove delivery timestamp)
        await _sut.UpsertAsync(
            new IdRegisterEntry
            {
                Sui = sui,
                CustodianId = custodianId,
                SystemId = systemId,
                RecordType = recordType,
                Provenance = Provenance.DiscoveredViaFanout,
                LastIdDeliveredAtUtc = null,
            },
            CancellationToken.None
        );

        var partitionKey = RegisterKeys.PartitionKey(sui);
        var rowKey = RegisterKeys.RowKey(custodianId, recordType, systemId);

        // ASSERT
        var entity = (
            await TableStorageFixture
                .Client.GetTableClient(InfrastructureConstants.StorageTableIdRegister.TableName)
                .GetEntityAsync<TableEntity>(partitionKey, rowKey)
        ).Value;

        entity.ContainsKey("LastIdDeliveredUtc").Should().BeFalse();
        entity.GetString("Provenance").Should().Be(Provenance.DiscoveredViaFanout.ToString());
    }

    [Fact]
    public async Task GetEntriesBySuiAsync_ReturnsPersistedEntries()
    {
        // ARRANGE
        var sui = $"Sui_{Guid.NewGuid()}";

        var entry = new IdRegisterEntry
        {
            Sui = sui,
            CustodianId = $"Custodian_{Guid.NewGuid()}",
            SystemId = $"System_{Guid.NewGuid()}",
            RecordType = $"RecordType_{Guid.NewGuid()}",
            CustodianSubjectId = $"Subject_{Guid.NewGuid()}",
            Provenance = Provenance.AlreadyHeldByCustodian,
            LastIdDeliveredAtUtc = null,
        };

        await _sut.UpsertAsync(entry, CancellationToken.None);

        // ACT
        var results = await _sut.GetEntriesBySuiAsync(sui, CancellationToken.None);

        // ASSERT
        results.Should().HaveCount(1);

        var result = results.Single();

        result.Sui.Should().Be(entry.Sui);
        result.CustodianId.Should().Be(entry.CustodianId);
        result.RecordType.Should().Be(entry.RecordType);
        result.SystemId.Should().Be(entry.SystemId);
        result.CustodianSubjectId.Should().Be(entry.CustodianSubjectId);
        result.Provenance.Should().Be(entry.Provenance);

        result.FirstSeenUtc.Should().NotBeNull();
        result.LastSeenUtc.Should().NotBeNull();
        result.LastIdDeliveredAtUtc.Should().BeNull();
    }

    [Fact]
    public async Task UpsertAsync_ExistingRow_UpdatesLastSeenUtc()
    {
        // ARRANGE
        var sui = $"Sui_{Guid.NewGuid()}";
        var custodianId = $"Custodian_{Guid.NewGuid()}";
        var systemId = $"System_{Guid.NewGuid()}";
        var recordType = $"RecordType_{Guid.NewGuid()}";

        var entry = new IdRegisterEntry
        {
            Sui = sui,
            CustodianId = custodianId,
            SystemId = systemId,
            RecordType = recordType,
            Provenance = Provenance.IssuedByService,
            LastIdDeliveredAtUtc = DateTimeOffset.UtcNow,
        };

        var partitionKey = RegisterKeys.PartitionKey(sui);
        var rowKey = RegisterKeys.RowKey(custodianId, recordType, systemId);

        // Act - First write
        await _sut.UpsertAsync(entry, CancellationToken.None);

        var tableClient = TableStorageFixture.Client.GetTableClient(
            InfrastructureConstants.StorageTableIdRegister.TableName
        );

        var firstEntity = (
            await tableClient.GetEntityAsync<TableEntity>(partitionKey, rowKey)
        ).Value;

        var firstSeen = firstEntity.GetDateTimeOffset("FirstSeenUtc");
        var firstLastSeen = firstEntity.GetDateTimeOffset("LastSeenUtc");

        await Task.Delay(10); // ensure timestamp difference

        // ACT - Second write
        await _sut.UpsertAsync(entry, CancellationToken.None);

        var secondEntity = (
            await tableClient.GetEntityAsync<TableEntity>(partitionKey, rowKey)
        ).Value;

        var secondFirstSeen = secondEntity.GetDateTimeOffset("FirstSeenUtc");
        var secondLastSeen = secondEntity.GetDateTimeOffset("LastSeenUtc");

        // ASSERT
        secondFirstSeen.Should().Be(firstSeen);
        secondLastSeen.Should().BeAfter(firstLastSeen!.Value);
    }

    [Fact]
    public async Task UpsertAsync_WhenSystemIdNullOrEmpty_UsesDefaultSystem()
    {
        // ARRANGE
        var entry = new IdRegisterEntry
        {
            Sui = $"Sui_{Guid.NewGuid()}",
            CustodianId = $"Custodian_{Guid.NewGuid()}",
            SystemId = string.Empty,
            RecordType = $"RecordType_{Guid.NewGuid()}",
            Provenance = Provenance.AlreadyHeldByCustodian,
        };

        var expectedPartitionKey = RegisterKeys.PartitionKey(entry.Sui);
        var expectedRowKey = RegisterKeys.RowKey(
            entry.CustodianId,
            entry.RecordType,
            "DefaultSystem"
        );

        // ACT
        await _sut.UpsertAsync(entry, CancellationToken.None);

        // ASSERT
        var entity = (
            await TableStorageFixture
                .Client.GetTableClient(InfrastructureConstants.StorageTableIdRegister.TableName)
                .GetEntityAsync<TableEntity>(expectedPartitionKey, expectedRowKey)
        ).Value;

        entity.GetString("SystemId").Should().Be("DefaultSystem");
    }

    [Fact]
    public async Task GetEntriesBySuiAsync_InvalidProvenance_DefaultsToUnknown()
    {
        // ARRANGE
        var sui = $"Sui_{Guid.NewGuid()}";
        var custodianId = $"Custodian_{Guid.NewGuid()}";
        var recordType = $"RecordType_{Guid.NewGuid()}";
        var systemId = $"System_{Guid.NewGuid()}";

        var partitionKey = RegisterKeys.PartitionKey(sui);
        var rowKey = RegisterKeys.RowKey(custodianId, recordType, systemId);

        var tableClient = TableStorageFixture.Client.GetTableClient(
            InfrastructureConstants.StorageTableIdRegister.TableName
        );

        var entity = new TableEntity(partitionKey, rowKey)
        {
            { "Sui", sui },
            { "CustodianId", custodianId },
            { "RecordType", recordType },
            { "SystemId", systemId },
            { "CustodianSubjectId", "Subject" },
            { "FirstSeenUtc", DateTimeOffset.UtcNow },
            { "LastSeenUtc", DateTimeOffset.UtcNow },
            { "Provenance", "InvalidValue" }, // corrupt value
        };

        await tableClient.UpsertEntityAsync(entity);

        // ACT
        var results = await _sut.GetEntriesBySuiAsync(sui, CancellationToken.None);

        // ASSERT
        results.Should().HaveCount(1);
        results.Single().Provenance.Should().Be(Provenance.Unknown);
    }
}
