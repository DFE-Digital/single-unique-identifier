using Azure.Data.Tables;
using Microsoft.Extensions.Logging.Abstractions;
using SUI.Find.Application.Dtos;
using SUI.Find.Infrastructure.Repositories.SearchResultEntryStorage;

namespace SUI.Find.Infrastructure.IntegrationTests.Repositories;

public class SearchResultEntryRepositoryTests : IAsyncLifetime
{
    private readonly SearchResultEntryRepository _sut = new(
        TableStorageFixture.Client,
        NullLogger<SearchResultEntryRepository>.Instance
    );

    public async Task InitializeAsync() =>
        await _sut.EnsureTableExistsAsync(CancellationToken.None);

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task UpsertAsync_CreatesNewEntry_AsExpected()
    {
        // ARRANGE
        var entry = new SearchResultEntry
        {
            CustodianId = $"Custodian_{Guid.NewGuid()}",
            SystemId = $"System_{Guid.NewGuid()}",
            SystemName = $"System_{Guid.NewGuid()}",
            RecordType = $"RecordType_{Guid.NewGuid()}",
            RecordUrl = "http://example.com",
            RecordId = "123",
            SubmittedAtUtc = DateTimeOffset.UtcNow,
            JobId = "job-1",
            WorkItemId = $"work_{Guid.NewGuid()}",
        };

        var partitionKey = SearchResultEntryKeys.PartitionKey(entry.WorkItemId);
        var rowKey = SearchResultEntryKeys.RowKey(
            entry.SubmittedAtUtc,
            entry.CustodianId,
            entry.RecordType,
            entry.SystemId
        );

        // ACT
        await _sut.UpsertAsync(entry, CancellationToken.None);

        // ASSERT
        var entity = (
            await TableStorageFixture
                .Client.GetTableClient(
                    InfrastructureConstants.StorageTableSearchResultEntries.TableName
                )
                .GetEntityAsync<TableEntity>(partitionKey, rowKey)
        ).Value;

        entity.GetString("CustodianId").Should().Be(entry.CustodianId);
        entity.GetString("SystemId").Should().Be(entry.SystemId);
        entity.GetString("RecordType").Should().Be(entry.RecordType);
        entity.GetString("RecordUrl").Should().Be(entry.RecordUrl);
        entity.GetString("RecordId").Should().Be(entry.RecordId);
        entity.GetString("JobId").Should().Be(entry.JobId);
        entity.GetString("WorkItemId").Should().Be(entry.WorkItemId);
    }

    [Fact]
    public async Task UpsertAsync_SameRow_ReplacesEntity()
    {
        var workItemId = $"work_{Guid.NewGuid()}";
        var submitted = DateTimeOffset.UtcNow;

        var entry = new SearchResultEntry
        {
            CustodianId = "CustodianA",
            SystemId = "SystemA",
            SystemName = "SystemA",
            RecordType = "Type1",
            RecordUrl = "url1",
            RecordId = "1",
            SubmittedAtUtc = submitted,
            JobId = "job",
            WorkItemId = workItemId,
        };

        await _sut.UpsertAsync(entry, CancellationToken.None);

        // Change URL
        var updatedEntry = new SearchResultEntry
        {
            CustodianId = "CustodianA",
            SystemId = "SystemA",
            SystemName = "SystemA",
            RecordType = "Type1",
            RecordUrl = "updated-url",
            RecordId = "1",
            SubmittedAtUtc = submitted,
            JobId = "job",
            WorkItemId = workItemId,
        };

        await _sut.UpsertAsync(updatedEntry, CancellationToken.None);

        var results = await _sut.GetByWorkItemIdAsync(workItemId);

        results.Should().HaveCount(1);
        results.Single().RecordUrl.Should().Be("updated-url");
    }

    [Fact]
    public async Task GetByWorkItemIdAsync_Deduplicates_ByCustodianSystemAndRecordType()
    {
        var workItemId = $"work_{Guid.NewGuid()}";

        var first = new SearchResultEntry
        {
            CustodianId = "CustodianA",
            SystemId = "SystemA",
            SystemName = "SystemA",
            RecordType = "Type1",
            RecordUrl = "url1",
            RecordId = "1",
            SubmittedAtUtc = DateTimeOffset.UtcNow,
            JobId = "job",
            WorkItemId = workItemId,
        };

        var second = new SearchResultEntry
        {
            CustodianId = "CustodianA",
            SystemId = "SystemA",
            SystemName = "SystemA",
            RecordType = "Type1",
            RecordUrl = "url2",
            RecordId = "2",
            SubmittedAtUtc = DateTimeOffset.UtcNow.AddSeconds(1),
            JobId = "job",
            WorkItemId = workItemId,
        };

        await _sut.UpsertAsync(first);
        await _sut.UpsertAsync(second);

        var results = await _sut.GetByWorkItemIdAsync(workItemId);

        results.Should().HaveCount(1);
    }

    [Fact]
    public async Task GetByWorkItemIdAsync_ReturnsChronologicalOrder()
    {
        var workItemId = $"work_{Guid.NewGuid()}";

        var earlier = new DateTimeOffset(2024, 01, 01, 10, 00, 00, TimeSpan.Zero);
        var later = new DateTimeOffset(2024, 01, 01, 10, 05, 00, TimeSpan.Zero);

        var first = new SearchResultEntry
        {
            CustodianId = "A",
            SystemId = "S",
            SystemName = "S",
            RecordType = "T1",
            RecordUrl = "url1",
            RecordId = "1",
            SubmittedAtUtc = earlier,
            JobId = "job",
            WorkItemId = workItemId,
        };

        var second = new SearchResultEntry
        {
            CustodianId = "B",
            SystemId = "S",
            SystemName = "S",
            RecordType = "T2",
            RecordUrl = "url2",
            RecordId = "2",
            SubmittedAtUtc = later,
            JobId = "job",
            WorkItemId = workItemId,
        };

        await _sut.UpsertAsync(second);
        await _sut.UpsertAsync(first);

        var results = await _sut.GetByWorkItemIdAsync(workItemId);

        results.First().RecordType.Should().Be("T1");
        results.Last().RecordType.Should().Be("T2");
    }

    [Fact]
    public async Task GetByWorkItemIdAsync_SkipsInvalidRows()
    {
        var workItemId = $"work_{Guid.NewGuid()}";
        var partitionKey = SearchResultEntryKeys.PartitionKey(workItemId);

        var tableClient = TableStorageFixture.Client.GetTableClient(
            InfrastructureConstants.StorageTableSearchResultEntries.TableName
        );

        var entity = new TableEntity(partitionKey, "bad-row")
        {
            { "CustodianId", null }, // corrupt
        };

        await tableClient.UpsertEntityAsync(entity);

        var results = await _sut.GetByWorkItemIdAsync(workItemId);

        results.Should().BeEmpty();
    }
}
