using Azure.Data.Tables;
using Microsoft.Extensions.Logging.Abstractions;
using SUI.Find.Infrastructure.Enums;
using SUI.Find.Infrastructure.Repositories.WorkItemJobCountRepository;

namespace SUI.Find.Infrastructure.IntegrationTests.Repositories;

public class WorkItemJobCountRepositoryTests : IAsyncLifetime
{
    private readonly WorkItemJobCountRepository _sut = new(
        TableStorageFixture.Client,
        NullLogger<WorkItemJobCountRepository>.Instance
    );

    public async Task InitializeAsync() =>
        await _sut.EnsureTableExistsAsync(CancellationToken.None);

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task UpsertAsync_CreatesNewRecord_AsExpected()
    {
        var workItemId = $"WI_{Guid.NewGuid()}";
        var jobType = JobType.CustodianLookup;
        var now = DateTimeOffset.UtcNow;

        var entity = new WorkItemJobCount
        {
            WorkItemId = workItemId,
            JobType = jobType,
            ExpectedJobCount = 5,
            CreatedAtUtc = now,
            UpdatedAtUtc = now,
            PayloadJson = "{}",
        };

        var partitionKey = WorkItemJobCountKeys.PartitionKey(workItemId);
        var rowKey = WorkItemJobCountKeys.RowKey(jobType);

        await _sut.UpsertAsync(entity);

        var tableClient = TableStorageFixture.Client.GetTableClient(
            InfrastructureConstants.StorageTableWorkItemJobCountRepository.TableName
        );

        var stored = await tableClient.GetEntityAsync<TableEntity>(partitionKey, rowKey);

        stored.Value.GetString("WorkItemId").Should().Be(workItemId);
        stored.Value.GetString("JobType").Should().Be(nameof(JobType.CustodianLookup));
        stored.Value.GetInt32("ExpectedJobCount").Should().Be(5);
        stored.Value.GetString("PayloadJson").Should().Be("{}");
    }

    [Fact]
    public async Task GetByWorkItemIdAndJobTypeAsync_ReturnsCount_WhenRecordExists()
    {
        var workItemId = $"WI_{Guid.NewGuid()}";
        var jobType = JobType.CustodianLookup;
        var now = DateTimeOffset.UtcNow;

        var entity = new WorkItemJobCount
        {
            WorkItemId = workItemId,
            JobType = jobType,
            ExpectedJobCount = 3,
            CreatedAtUtc = now,
            UpdatedAtUtc = now,
            PayloadJson = "{}",
        };

        await _sut.UpsertAsync(entity);

        var result = await _sut.GetByWorkItemIdAndJobTypeAsync(workItemId, jobType);

        result.Should().NotBeNull();
        result.ExpectedJobCount.Should().Be(3);
        result.WorkItemId.Should().Be(workItemId);
        result.JobType.Should().Be(jobType);
    }

    [Fact]
    public async Task GetByWorkItemIdAndJobTypeAsync_ReturnsNull_WhenRecordDoesNotExist()
    {
        var count = await _sut.GetByWorkItemIdAndJobTypeAsync(
            $"WI_{Guid.NewGuid()}",
            JobType.CustodianLookup
        );

        count.Should().BeNull();
    }

    [Fact]
    public async Task UpsertAsync_ReplacesExistingRecord()
    {
        var workItemId = $"WI_{Guid.NewGuid()}";
        var jobType = JobType.CustodianLookup;
        var now = DateTimeOffset.UtcNow;

        var entity = new WorkItemJobCount
        {
            WorkItemId = workItemId,
            JobType = jobType,
            ExpectedJobCount = 1,
            CreatedAtUtc = now,
            UpdatedAtUtc = now,
            PayloadJson = "{}",
        };

        await _sut.UpsertAsync(entity);

        var updated = new WorkItemJobCount
        {
            WorkItemId = workItemId,
            JobType = jobType,
            ExpectedJobCount = 10,
            CreatedAtUtc = now,
            UpdatedAtUtc = DateTimeOffset.UtcNow,
            PayloadJson = "{}",
        };

        await _sut.UpsertAsync(updated);

        var result = await _sut.GetByWorkItemIdAndJobTypeAsync(workItemId, jobType);

        result.Should().NotBeNull();
        result.ExpectedJobCount.Should().Be(10);
    }
}
