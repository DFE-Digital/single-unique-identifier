using Azure.Data.Tables;
using Microsoft.Extensions.Logging.Abstractions;
using SUI.Find.Infrastructure.Repositories.JobRepository;

namespace SUI.Find.Infrastructure.IntegrationTests.Repositories;

public class JobRepositoryTests : IAsyncLifetime
{
    private readonly JobRepository _sut = new(
        TableStorageFixture.Client,
        NullLogger<JobRepository>.Instance
    );

    public async Task InitializeAsync() =>
        await _sut.EnsureTableExistsAsync(CancellationToken.None);

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task UpsertAsync_CreatesNewJob_AsExpected()
    {
        // ARRANGE
        var custodianId = $"Custodian_{Guid.NewGuid()}";
        var jobId = Guid.NewGuid().ToString();
        var createdAt = DateTimeOffset.UtcNow;

        var job = new Job
        {
            JobId = jobId,
            CustodianId = custodianId,
            JobType = JobType.CustodianLookup,
            WorkItemType = WorkItemType.SearchExecution,
            WorkItemId = $"WI_{Guid.NewGuid()}",
            LeaseId = "LEASE1",
            LeaseExpiresAtUtc = createdAt.AddMinutes(5),
            AttemptCount = 1,
            CreatedAtUtc = createdAt,
            UpdatedAtUtc = createdAt,
            CompletedAtUtc = null,
            PayloadJson = "{}",
            JobTraceParent = "trace123",
        };

        var partitionKey = JobKeys.PartitionKey(job.CustodianId);
        var rowKey = JobKeys.RowKey(job.CreatedAtUtc, job.JobId);

        // ACT
        await _sut.UpsertAsync(job, CancellationToken.None);

        // ASSERT
        var entity = (
            await TableStorageFixture
                .Client.GetTableClient(InfrastructureConstants.StorageTableJobRepository.TableName)
                .GetEntityAsync<TableEntity>(partitionKey, rowKey)
        ).Value;

        entity.GetString("JobId").Should().Be(job.JobId);
        entity.GetString("CustodianId").Should().Be(job.CustodianId);
        entity.GetString("JobType").Should().Be(job.JobType.ToString());
        entity.GetString("WorkItemType").Should().Be(job.WorkItemType.ToString());
        entity.GetString("WorkItemId").Should().Be(job.WorkItemId);
    }

    [Fact]
    public async Task ListJobsByCustodianIdAsync_FiltersByWindowStart()
    {
        // ARRANGE
        var custodianId = $"Custodian_{Guid.NewGuid()}";

        var oldJob = new Job
        {
            JobId = "old-job",
            CustodianId = custodianId,
            JobType = JobType.CustodianLookup,
            WorkItemType = WorkItemType.SearchExecution,
            CreatedAtUtc = DateTimeOffset.UtcNow.AddHours(-2),
            UpdatedAtUtc = DateTimeOffset.UtcNow.AddHours(-2),
            PayloadJson = "{}",
        };

        var newJob = new Job
        {
            JobId = "new-job",
            CustodianId = custodianId,
            JobType = JobType.CustodianLookup,
            WorkItemType = WorkItemType.SearchExecution,
            CreatedAtUtc = DateTimeOffset.UtcNow,
            UpdatedAtUtc = DateTimeOffset.UtcNow,
            PayloadJson = "{}",
        };

        await _sut.UpsertAsync(oldJob);
        await _sut.UpsertAsync(newJob);

        var windowStart = DateTimeOffset.UtcNow.AddHours(-1);

        // ACT
        var jobs = await _sut.ListJobsByCustodianIdAsync(custodianId, windowStart);

        // ASSERT
        jobs.Should().HaveCount(1);
        jobs.First().JobId.Should().Be("new-job");
    }

    [Fact]
    public async Task ListJobsByCustodianIdAsync_ReturnsChronologicalOrder()
    {
        // ARRANGE
        var custodianId = $"Custodian_{Guid.NewGuid()}";

        var earlier = DateTimeOffset.UtcNow.AddMinutes(-10);
        var later = DateTimeOffset.UtcNow;

        var first = new Job
        {
            JobId = "first-job",
            CustodianId = custodianId,
            JobType = JobType.CustodianLookup,
            WorkItemType = WorkItemType.SearchExecution,
            CreatedAtUtc = earlier,
            UpdatedAtUtc = earlier,
            PayloadJson = "{}",
        };

        var second = new Job
        {
            JobId = "second-job",
            CustodianId = custodianId,
            JobType = JobType.CustodianLookup,
            WorkItemType = WorkItemType.SearchExecution,
            CreatedAtUtc = later,
            UpdatedAtUtc = later,
            PayloadJson = "{}",
        };

        await _sut.UpsertAsync(second);
        await _sut.UpsertAsync(first);

        // ACT
        var jobs = await _sut.ListJobsByCustodianIdAsync(
            custodianId,
            DateTimeOffset.UtcNow.AddHours(-1)
        );

        // ASSERT
        jobs.First().JobId.Should().Be("first-job");
        jobs.Last().JobId.Should().Be("second-job");
    }

    [Fact]
    public async Task ListJobsByCustodianIdAsync_SkipsInvalidRows()
    {
        // ARRANGE
        var custodianId = $"Custodian_{Guid.NewGuid()}";
        var partitionKey = JobKeys.PartitionKey(custodianId);

        var tableClient = TableStorageFixture.Client.GetTableClient(
            InfrastructureConstants.StorageTableJobRepository.TableName
        );

        // insert a bad row
        var entity = new TableEntity(partitionKey, "bad-row")
        {
            { "JobId", null }, // corrupt row
        };

        await tableClient.UpsertEntityAsync(entity);

        // ACT
        var jobs = await _sut.ListJobsByCustodianIdAsync(
            custodianId,
            DateTimeOffset.UtcNow.AddHours(-1)
        );

        // ASSERT
        jobs.Should().BeEmpty();
    }
}
