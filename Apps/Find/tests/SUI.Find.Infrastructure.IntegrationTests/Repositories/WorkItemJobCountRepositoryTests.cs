using System.Text.RegularExpressions;
using Azure.Data.Tables;
using Microsoft.Extensions.Logging.Abstractions;
using SUI.Find.Application.Enums;
using SUI.Find.Infrastructure.Repositories.WorkItemJobCountRepository;

namespace SUI.Find.Infrastructure.IntegrationTests.Repositories;

public partial class WorkItemJobCountRepositoryTests : IAsyncLifetime
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
        var searchingOrganisationId = $"SOID_{Guid.NewGuid()}";
        var jobType = JobType.CustodianLookup;
        var now = DateTimeOffset.UtcNow;

        var entity = new WorkItemJobCount
        {
            WorkItemId = workItemId,
            JobType = jobType,
            ExpectedJobCount = 5,
            CreatedAtUtc = now,
            UpdatedAtUtc = now,
            SearchingOrganisationId = searchingOrganisationId,
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
        stored.Value.GetString("SearchingOrganisationId").Should().Be(searchingOrganisationId);
        stored.Value.GetInt32("ExpectedJobCount").Should().Be(5);
        stored.Value.GetString("PayloadJson").Should().Be("{}");
    }

    [Fact]
    public async Task GetByWorkItemIdAndJobTypeAsync_ReturnsCount_WhenRecordExists()
    {
        var workItemId = $"WI_{Guid.NewGuid()}";
        var searchingOrganisationId = $"SOID_{Guid.NewGuid()}";
        var jobType = JobType.CustodianLookup;
        var now = DateTimeOffset.UtcNow;

        var entity = new WorkItemJobCount
        {
            WorkItemId = workItemId,
            JobType = jobType,
            ExpectedJobCount = 3,
            CreatedAtUtc = now,
            UpdatedAtUtc = now,
            SearchingOrganisationId = searchingOrganisationId,
            PayloadJson = "{}",
        };

        await _sut.UpsertAsync(entity);

        var result = await _sut.GetByWorkItemIdAndJobTypeAsync(workItemId, jobType);

        result.Should().NotBeNull();
        result.ExpectedJobCount.Should().Be(3);
        result.WorkItemId.Should().Be(workItemId);
        result.SearchingOrganisationId.Should().Be(searchingOrganisationId);
        result.JobType.Should().Be(jobType);
        result.CreatedAtUtc.Should().Be(now);
        result.UpdatedAtUtc.Should().Be(now);
        result.PayloadJson.Should().Be("{}");
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
        var searchingOrganisationId = $"SOID_{Guid.NewGuid()}";
        var jobType = JobType.CustodianLookup;
        var now = DateTimeOffset.UtcNow;

        var entity = new WorkItemJobCount
        {
            WorkItemId = workItemId,
            JobType = jobType,
            ExpectedJobCount = 1,
            CreatedAtUtc = now,
            UpdatedAtUtc = now,
            SearchingOrganisationId = searchingOrganisationId,
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
            SearchingOrganisationId = searchingOrganisationId,
            PayloadJson = "{}",
        };

        await _sut.UpsertAsync(updated);

        var result = await _sut.GetByWorkItemIdAndJobTypeAsync(workItemId, jobType);

        result.Should().NotBeNull();
        result.ExpectedJobCount.Should().Be(10);
    }

    [Fact]
    public async Task GetByWorkItemIdAndJobTypeAsync_ReturnsUnknownJobType_WhenStoredStringIsInvalid()
    {
        var workItemId = $"WI_{Guid.NewGuid()}";
        var searchingOrganisationId = $"SOID_{Guid.NewGuid()}";
        var lookupJobType = JobType.CustodianLookup; // Used to generate the RowKey so our Get method can find it
        var now = DateTimeOffset.UtcNow;

        var partitionKey = WorkItemJobCountKeys.PartitionKey(workItemId);
        var rowKey = WorkItemJobCountKeys.RowKey(lookupJobType);

        var tableClient = TableStorageFixture.Client.GetTableClient(
            InfrastructureConstants.StorageTableWorkItemJobCountRepository.TableName
        );

        // Directly insert an entity with an invalid JobType string to simulate corrupted/legacy data
        var invalidEntity = new TableEntity(partitionKey, rowKey)
        {
            { "WorkItemId", workItemId },
            { "JobType", "SomeInvalidJobType" }, // Invalid JobType
            { "ExpectedJobCount", 5 },
            { "CreatedAtUtc", now },
            { "UpdatedAtUtc", now },
            { "SearchingOrganisationId", searchingOrganisationId },
            { "PayloadJson", "{}" },
        };

        await tableClient.AddEntityAsync(invalidEntity);

        var result = await _sut.GetByWorkItemIdAndJobTypeAsync(workItemId, lookupJobType);

        result.Should().NotBeNull();
        result.WorkItemId.Should().Be(workItemId);
        result.JobType.Should().Be(JobType.Unknown);
        result.ExpectedJobCount.Should().Be(5);
    }

    [Fact]
    public async Task SearchingOrganisationId_IsOptional()
    {
        var workItemId = $"WI_{Guid.NewGuid()}";
        const JobType jobType = JobType.CustodianLookup;
        var now = DateTimeOffset.UtcNow;

        var entity = new WorkItemJobCount
        {
            SearchingOrganisationId = null,
            WorkItemId = workItemId,
            JobType = jobType,
            ExpectedJobCount = 5,
            CreatedAtUtc = now,
            UpdatedAtUtc = now,
            PayloadJson = "{}",
        };

        await _sut.UpsertAsync(entity);

        // ACT
        var result = await _sut.GetByWorkItemIdAndJobTypeAsync(workItemId, jobType);

        // ASSERT
        result
            .Should()
            .BeEquivalentTo(
                new { WorkItemId = workItemId, SearchingOrganisationId = (string?)null }
            );
    }

    [Fact]
    public async Task MarkJobCompletedAsync_CanAtomically_Update_CompletedJobIds_HashSet()
    {
        var workItemId = $"WI_{Guid.NewGuid()}";
        var searchingOrganisationId = $"SOID_{Guid.NewGuid()}";
        const JobType jobType = JobType.CustodianLookup;
        var now = DateTimeOffset.UtcNow;

        var entity = new WorkItemJobCount
        {
            WorkItemId = workItemId,
            JobType = jobType,
            ExpectedJobCount = 3,
            CreatedAtUtc = now,
            UpdatedAtUtc = now,
            SearchingOrganisationId = searchingOrganisationId,
            PayloadJson = "{}",
        };

        await _sut.UpsertAsync(entity);

        var testCompletedJobIds = Enumerable
            .Range(0, 100)
            .Select(_ => Guid.NewGuid().ToString())
            .ToArray();

        // ACT
        await Parallel.ForEachAsync(
            testCompletedJobIds,
            async (completedJobId, cancellationToken) =>
            {
                await _sut.MarkJobCompletedAsync(
                    workItemId,
                    jobType,
                    completedJobId,
                    cancellationToken
                );
            }
        );

        // ASSERT
        var result = await _sut.GetByWorkItemIdAndJobTypeAsync(workItemId, jobType);

        result.Should().NotBeNull();
        result.WorkItemId.Should().Be(workItemId);
        result.CompletedJobIds.Should().HaveCount(100);
        result.CompletedJobIds.Should().BeEquivalentTo(testCompletedJobIds);

        // Verify that all the properties of the storage entry have valid names
        var partitionKey = WorkItemJobCountKeys.PartitionKey(workItemId);
        var rowKey = WorkItemJobCountKeys.RowKey(jobType);
        var tableClient = TableStorageFixture.Client.GetTableClient(
            InfrastructureConstants.StorageTableWorkItemJobCountRepository.TableName
        );
        var stored = await tableClient.GetEntityAsync<TableEntity>(partitionKey, rowKey);

        stored
            .Value.Keys.Where(key => !key.StartsWith("odata"))
            .Should()
            .AllSatisfy(key =>
                ValidIdentifierRegex()
                    .IsMatch(key)
                    .Should()
                    .BeTrue($"the storage property name {key} should be a valid identifier")
            );
    }

    [GeneratedRegex(@"^[a-zA-Z_]\w+$")]
    private static partial Regex ValidIdentifierRegex();
}
