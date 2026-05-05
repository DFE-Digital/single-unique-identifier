using Azure;
using Azure.Data.Tables;
using Azure.Data.Tables.Models;
using Microsoft.Extensions.Logging.Abstractions;
using SUI.Find.Application.Enums;
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
        var createdAt = DateTimeOffset.UtcNow;

        var job = new Job
        {
            JobId = $"Job_{Guid.NewGuid()}",
            CustodianId = $"Custodian_{Guid.NewGuid()}",
            RequestingOrganisationId = $"RequestingOrganisation_{Guid.NewGuid()}",
            JobType = JobType.CustodianLookup,
            WorkItemType = WorkItemType.SearchExecution,
            WorkItemId = $"WI_{Guid.NewGuid()}",
            LeaseId = "LEASE1",
            LeaseExpiresAtUtc = createdAt.AddMinutes(5),
            AttemptCount = 1,
            CreatedAtUtc = createdAt,
            UpdatedAtUtc = createdAt,
            CompletedAtUtc = createdAt.AddMinutes(10),
            PayloadJson = "{}",
            JobTraceParent = "trace123",
        };

        await _sut.UpsertAsync(job);

        var entity = (
            await TableStorageFixture
                .Client.GetTableClient(InfrastructureConstants.StorageTableJobRepository.TableName)
                .GetEntityAsync<TableEntity>(
                    JobKeys.PartitionKey(job.CustodianId),
                    JobKeys.RowKey(job.CreatedAtUtc, job.JobId)
                )
        ).Value;

        entity.GetString("JobId").Should().Be(job.JobId);
        entity.GetString("CustodianId").Should().Be(job.CustodianId);
        entity.GetString("RequestingOrganisationId").Should().Be(job.RequestingOrganisationId);
        entity.GetString("JobType").Should().Be(job.JobType.ToString());
        entity.GetString("WorkItemType").Should().Be(job.WorkItemType.ToString());
        entity.GetString("WorkItemId").Should().Be(job.WorkItemId);
        entity.GetDateTimeOffset("CompletedAtUtc").Should().Be(job.CompletedAtUtc);
    }

    [Fact]
    public async Task UpdateAsync_UpdatesExistingJob_AsExpected()
    {
        var createdAt = DateTimeOffset.UtcNow.AddMinutes(-10);

        var custodianId = $"Custodian_{Guid.NewGuid()}";

        var job = new Job
        {
            JobId = $"Job_{Guid.NewGuid()}",
            CustodianId = custodianId,
            RequestingOrganisationId = $"RequestingOrganisation_{Guid.NewGuid()}",
            JobType = JobType.CustodianLookup,
            WorkItemType = WorkItemType.SearchExecution,
            WorkItemId = $"WI_{Guid.NewGuid()}",
            CreatedAtUtc = createdAt,
            UpdatedAtUtc = createdAt,
            PayloadJson = "{}",
        };

        await _sut.UpsertAsync(job);

        var updateRequest = job with
        {
            LeaseId = "updated-LeaseId",
            LeaseExpiresAtUtc = createdAt.AddMinutes(30),
            AttemptCount = 1234,
            UpdatedAtUtc = createdAt.AddMinutes(1),
            CompletedAtUtc = createdAt.AddMinutes(2),
            JobTraceParent = "updated-JobTraceParent",
        };

        // ACT
        await _sut.UpdateAsync(updateRequest, ifMatchETag: "*");

        // ASSERT
        var entity = (
            await TableStorageFixture
                .Client.GetTableClient(InfrastructureConstants.StorageTableJobRepository.TableName)
                .GetEntityAsync<TableEntity>(
                    JobKeys.PartitionKey(job.CustodianId),
                    JobKeys.RowKey(job.CreatedAtUtc, job.JobId)
                )
        ).Value;

        entity.GetString("JobId").Should().Be(job.JobId);
        entity.GetString("CustodianId").Should().Be(job.CustodianId);
        entity.GetString("RequestingOrganisationId").Should().Be(job.RequestingOrganisationId);

        entity.GetString("LeaseId").Should().Be("updated-LeaseId");
        entity.GetDateTimeOffset("LeaseExpiresAtUtc").Should().Be(createdAt.AddMinutes(30));
        entity.GetInt32("AttemptCount").Should().Be(1234);
        entity.GetDateTimeOffset("UpdatedAtUtc").Should().Be(createdAt.AddMinutes(1));
        entity.GetDateTimeOffset("CompletedAtUtc").Should().Be(createdAt.AddMinutes(2));
        entity.GetString("JobTraceParent").Should().Be("updated-JobTraceParent");
    }

    [Fact]
    public async Task UpdateAsync_ThrowsExpectedError_WhenTheJobIsUpdatedElsewhereConcurrently()
    {
        var job = new Job
        {
            JobId = $"Job_{Guid.NewGuid()}",
            CustodianId = $"Custodian_{Guid.NewGuid()}",
            RequestingOrganisationId = $"RequestingOrganisation_{Guid.NewGuid()}",
            JobType = JobType.CustodianLookup,
            WorkItemType = WorkItemType.SearchExecution,
            WorkItemId = $"WI_{Guid.NewGuid()}",
            CreatedAtUtc = DateTimeOffset.UtcNow,
            UpdatedAtUtc = DateTimeOffset.UtcNow,
            PayloadJson = "{}",
        };

        await _sut.UpsertAsync(job);

        var originalETag = (await GetEntityAsync()).ETag.ToString();
        originalETag.Should().NotBe("*");

        var updateRequest = job with { LeaseId = "updated-LeaseId", ETag = originalETag };

        // Simulate a different invocation updating the Job concurrently
        await _sut.UpdateAsync(updateRequest, ifMatchETag: originalETag);

        // ACT & ASSERT
        var action = async () => await _sut.UpdateAsync(updateRequest, ifMatchETag: originalETag);

        (await action.Should().ThrowAsync<RequestFailedException>())
            .And.Should()
            .BeEquivalentTo(
                new
                {
                    Status = 412,
                    ErrorCode = TableErrorCode.UpdateConditionNotSatisfied.ToString(),
                }
            );

        return;

        async Task<TableEntity> GetEntityAsync() =>
            (
                await TableStorageFixture
                    .Client.GetTableClient(
                        InfrastructureConstants.StorageTableJobRepository.TableName
                    )
                    .GetEntityAsync<TableEntity>(
                        JobKeys.PartitionKey(job.CustodianId),
                        JobKeys.RowKey(job.CreatedAtUtc, job.JobId)
                    )
            ).Value;
    }

    [Fact]
    public async Task ListJobsByCustodianIdAsync_FiltersByWindowStart()
    {
        var custodianId = $"Custodian_{Guid.NewGuid()}";

        var oldJob = new Job
        {
            JobId = "old-job",
            CustodianId = custodianId,
            RequestingOrganisationId = $"RequestingOrganisation_{Guid.NewGuid()}",
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
            RequestingOrganisationId = $"RequestingOrganisation_{Guid.NewGuid()}",
            JobType = JobType.CustodianLookup,
            WorkItemType = WorkItemType.SearchExecution,
            CreatedAtUtc = DateTimeOffset.UtcNow,
            UpdatedAtUtc = DateTimeOffset.UtcNow,
            PayloadJson = "{}",
        };

        await _sut.UpsertAsync(oldJob);
        await _sut.UpsertAsync(newJob);

        var windowStart = DateTimeOffset.UtcNow.AddHours(-1);

        var jobs = await _sut.ListJobsByCustodianIdAsync(custodianId, windowStart);

        jobs.Should().HaveCount(1);
        jobs[0].JobId.Should().Be("new-job");
    }

    [Fact]
    public async Task ListJobsByCustodianIdAsync_ReturnsChronologicalOrder()
    {
        var custodianId = $"Custodian_{Guid.NewGuid()}";

        var earlier = DateTimeOffset.UtcNow.AddMinutes(-10);
        var later = DateTimeOffset.UtcNow;

        var first = new Job
        {
            JobId = "first-job",
            CustodianId = custodianId,
            RequestingOrganisationId = $"RequestingOrganisation_{Guid.NewGuid()}",
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
            RequestingOrganisationId = $"RequestingOrganisation_{Guid.NewGuid()}",
            JobType = JobType.CustodianLookup,
            WorkItemType = WorkItemType.SearchExecution,
            CreatedAtUtc = later,
            UpdatedAtUtc = later,
            PayloadJson = "{}",
        };

        await _sut.UpsertAsync(second);
        await _sut.UpsertAsync(first);

        var jobs = await _sut.ListJobsByCustodianIdAsync(
            custodianId,
            DateTimeOffset.UtcNow.AddHours(-1)
        );

        jobs[0].JobId.Should().Be("first-job");
        jobs[1].JobId.Should().Be("second-job");
    }

    [Fact]
    public async Task ListJobsByCustodianIdAsync_SkipsInvalidRows()
    {
        var custodianId = $"Custodian_{Guid.NewGuid()}";
        var partitionKey = JobKeys.PartitionKey(custodianId);

        var tableClient = TableStorageFixture.Client.GetTableClient(
            InfrastructureConstants.StorageTableJobRepository.TableName
        );

        var entity = new TableEntity(partitionKey, "bad-row")
        {
            { "JobId", null }, // corrupt row
        };

        await tableClient.UpsertEntityAsync(entity);

        var jobs = await _sut.ListJobsByCustodianIdAsync(
            custodianId,
            DateTimeOffset.UtcNow.AddHours(-1)
        );

        jobs.Should().BeEmpty();
    }

    [Fact]
    public async Task ListJobsByCustodianIdAsync_InvalidEnum_MapsToUnknown()
    {
        var custodianId = $"Custodian_{Guid.NewGuid()}";
        var jobId = $"Job_{Guid.NewGuid()}";
        var createdAt = DateTimeOffset.UtcNow;

        var entity = new TableEntity(
            JobKeys.PartitionKey(custodianId),
            JobKeys.RowKey(createdAt, jobId)
        )
        {
            { "JobId", jobId },
            { "CustodianId", custodianId },
            { "JobType", "INVALID" },
            { "WorkItemType", "INVALID" },
            { "CreatedAtUtc", createdAt },
            { "UpdatedAtUtc", createdAt },
            { "PayloadJson", "{}" },
        };

        var tableClient = TableStorageFixture.Client.GetTableClient(
            InfrastructureConstants.StorageTableJobRepository.TableName
        );

        await tableClient.UpsertEntityAsync(entity);

        var jobs = await _sut.ListJobsByCustodianIdAsync(
            custodianId,
            DateTimeOffset.UtcNow.AddHours(-1)
        );

        jobs.Should().HaveCount(1);
        jobs[0].JobType.Should().Be(JobType.Unknown);
        jobs[0].WorkItemType.Should().Be(WorkItemType.Unknown);
    }

    [Fact]
    public async Task ListJobsByCustodianIdAsync_DoesReturnAllApplicableJobs()
    {
        var windowStart = DateTimeOffset.UtcNow.AddHours(-1);
        var custodianId = $"Custodian_{Guid.NewGuid()}";

        string[] jobIds =
        [
            "a-job",
            "07ac4f10-65f7-4802-88bd-822671cc9dc7",
            "ab5e66ee-99a5-482f-9cc2-285e28410418",
            "z-job",
        ];

        await Task.WhenAll(
            jobIds
                .Select(jobId => new Job
                {
                    JobId = jobId,
                    CustodianId = custodianId,
                    RequestingOrganisationId = $"RequestingOrganisation_{Guid.NewGuid()}",
                    JobType = JobType.CustodianLookup,
                    WorkItemType = WorkItemType.SearchExecution,
                    CreatedAtUtc = windowStart,
                    UpdatedAtUtc = DateTimeOffset.UtcNow,
                    PayloadJson = "{}",
                })
                .Select(job => _sut.UpsertAsync(job))
        );

        // ACT
        var jobs = await _sut.ListJobsByCustodianIdAsync(custodianId, windowStart);

        // ASSERT
        jobs.Select(x => x.JobId).Should().BeEquivalentTo(jobIds);
    }
}
