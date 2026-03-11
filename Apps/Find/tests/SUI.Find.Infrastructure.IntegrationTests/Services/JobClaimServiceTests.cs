using Azure;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;
using SUI.Find.Application.Dtos;
using SUI.Find.Application.Interfaces;
using SUI.Find.Infrastructure.Configuration;
using SUI.Find.Infrastructure.Enums;
using SUI.Find.Infrastructure.Repositories.JobRepository;
using SUI.Find.Infrastructure.Services;

namespace SUI.Find.Infrastructure.IntegrationTests.Services;

/// <remarks>
/// These tests are intentionally behavioural integration tests,to ease the migration away from Azure Storage to the Alpha storage choice.
/// </remarks>
public class JobClaimServiceTests : IAsyncLifetime
{
    private readonly JobRepository _jobRepository = new(
        TableStorageFixture.Client,
        NullLogger<JobRepository>.Instance
    );

    private readonly IJobWindowStartService _mockJobWindowStartService =
        Substitute.For<IJobWindowStartService>();
    private readonly IOptionsMonitor<JobClaimConfig> _mockOptions = Substitute.For<
        IOptionsMonitor<JobClaimConfig>
    >();
    private readonly TimeProvider _mockTimeProvider = Substitute.For<TimeProvider>();
    private readonly ILogger<JobClaimService> _mockLogger = Substitute.For<
        ILogger<JobClaimService>
    >();

    private JobClaimService _sut;

    public JobClaimServiceTests()
    {
        _mockOptions.CurrentValue.Returns(new JobClaimConfig());

        _sut = new JobClaimService(
            _jobRepository,
            _mockJobWindowStartService,
            _mockOptions,
            _mockTimeProvider,
            _mockLogger
        );
    }

    public async Task InitializeAsync() =>
        await _jobRepository.EnsureTableExistsAsync(CancellationToken.None);

    public Task DisposeAsync() => Task.CompletedTask;

    private async Task UpsertJobsAsync(params IEnumerable<Job> jobs) =>
        await Task.WhenAll(jobs.Select(job => _jobRepository.UpsertAsync(job)));

    [Fact]
    public async Task ClaimNextAvailableJobAsync_ReturnsNull_When_NoJobsWaiting()
    {
        var custodianId = $"custodian-{Guid.NewGuid()}";

        // ACT
        var result = await _sut.ClaimNextAvailableJobAsync(custodianId);

        // ASSERT
        result.Should().BeNull();
    }

    [Fact]
    public async Task ClaimNextAvailableJobAsync_ReturnsNull_When_AllJobsCompleted()
    {
        var custodianId = $"custodian-{Guid.NewGuid()}";

        await UpsertJobsAsync(
            Enumerable
                .Range(0, 3)
                .Select(i => new Job
                {
                    CompletedAtUtc = DateTimeOffset.UtcNow.AddMinutes(-1), // Job is completed
                    JobId = $"job-{i}-{custodianId}",
                    CustodianId = custodianId,
                    JobType = default,
                    PayloadJson = "",
                })
        );

        // ACT
        var result = await _sut.ClaimNextAvailableJobAsync(custodianId);

        // ASSERT
        result.Should().BeNull();
    }

    [Fact]
    public async Task ClaimNextAvailableJobAsync_ReturnsNull_When_JobHasBeenAttemptedMaximumNumberOfTimes()
    {
        var custodianId = $"custodian-{Guid.NewGuid()}";

        await UpsertJobsAsync(
            Enumerable
                .Range(0, 3)
                .Select(i => new Job
                {
                    AttemptCount = _mockOptions.CurrentValue.MaxClaimAttemptsPerJob + i, // Job has been attempted maximum number of times
                    JobId = $"job-{i}-{custodianId}",
                    CustodianId = custodianId,
                    JobType = default,
                    PayloadJson = "",
                })
        );

        // ACT
        var result = await _sut.ClaimNextAvailableJobAsync(custodianId);

        // ASSERT
        result.Should().BeNull();
    }

    [Fact]
    public async Task ClaimNextAvailableJobAsync_ReturnsNull_When_JobIsStillLeased()
    {
        var custodianId = $"custodian-{Guid.NewGuid()}";

        _mockTimeProvider.GetUtcNow().Returns(DateTimeOffset.UtcNow);

        await UpsertJobsAsync(
            Enumerable
                .Range(0, 3)
                .Select(i => new Job
                {
                    LeaseExpiresAtUtc = DateTimeOffset.UtcNow.AddMinutes(10), // Job is still leased
                    JobId = $"job-{i}-{custodianId}",
                    CustodianId = custodianId,
                    JobType = default,
                    PayloadJson = "",
                })
        );

        // ACT
        var result = await _sut.ClaimNextAvailableJobAsync(custodianId);

        // ASSERT
        result.Should().BeNull();
    }

    [Fact]
    public async Task ClaimNextAvailableJobAsync_ReturnsNull_When_JobIsTooOld()
    {
        var custodianId = $"custodian-{Guid.NewGuid()}";

        _mockJobWindowStartService.GetWindowStart().Returns(DateTimeOffset.UtcNow.AddHours(-10));

        await UpsertJobsAsync(
            Enumerable
                .Range(0, 3)
                .Select(i => new Job
                {
                    CreatedAtUtc = _mockJobWindowStartService.GetWindowStart().AddMilliseconds(-1), // Job is too old, falls outside of window
                    JobId = $"job-{i}-{custodianId}",
                    CustodianId = custodianId,
                    JobType = default,
                    PayloadJson = "",
                })
        );

        // ACT
        var result = await _sut.ClaimNextAvailableJobAsync(custodianId);

        // ASSERT
        result.Should().BeNull();
    }

    [Fact]
    public async Task ClaimNextAvailableJobAsync_ClaimsNextAvailableJob_WithEarliestCreatedDate()
    {
        var custodianId = $"custodian-{Guid.NewGuid()}";

        _mockTimeProvider.GetUtcNow().Returns(DateTimeOffset.UtcNow);
        _mockJobWindowStartService.GetWindowStart().Returns(DateTimeOffset.UtcNow.AddHours(-10));

        await UpsertJobsAsync(
            new Job
            {
                CreatedAtUtc = _mockJobWindowStartService.GetWindowStart().AddHours(1),
                JobId = $"job-a-{custodianId}",
                WorkItemId = $"wi-{custodianId}",
                CustodianId = custodianId,
                JobType = default,
                PayloadJson = "",
            },
            new Job
            {
                CreatedAtUtc = _mockJobWindowStartService.GetWindowStart().AddHours(0.5), // This job should be chosen because it has the earliest creation date
                JobId = $"job-x-{custodianId}",
                WorkItemId = $"wi-{custodianId}",
                CustodianId = custodianId,
                JobType = default,
                PayloadJson = "",
            }
        );

        // ACT
        var result = await _sut.ClaimNextAvailableJobAsync(custodianId);

        // ASSERT
        result.Should().NotBeNull();
        result.JobId.Should().Be($"job-x-{custodianId}");

        var dbJobs = await _jobRepository.ListJobsByCustodianIdAsync(
            custodianId,
            DateTimeOffset.MinValue
        );

        var jobX = dbJobs.Single(x => x.JobId == $"job-x-{custodianId}");
        var jobA = dbJobs.Single(x => x.JobId == $"job-a-{custodianId}");

        jobX.LeaseId.Should().NotBeNull();
        jobX.LeaseExpiresAtUtc.Should()
            .Be(
                _mockTimeProvider.GetUtcNow()
                    + TimeSpan.FromMinutes(_mockOptions.CurrentValue.LeaseDurationMinutes)
            );
        jobX.AttemptCount.Should().Be(1);
        jobX.UpdatedAtUtc.Should().Be(_mockTimeProvider.GetUtcNow());

        jobA.LeaseId.Should().BeNull();
        jobA.LeaseExpiresAtUtc.Should().BeNull();
        jobA.AttemptCount.Should().Be(0);
        jobA.UpdatedAtUtc.Should().BeBefore(_mockTimeProvider.GetUtcNow());

        result
            .Should()
            .BeEquivalentTo(
                new JobInfo
                {
                    JobId = $"job-x-{custodianId}",
                    CustodianId = custodianId,
                    LeaseExpiresAtUtc = jobX.LeaseExpiresAtUtc.Value,
                    LeaseId = jobX.LeaseId,
                    WorkItemId = $"wi-{custodianId}",
                }
            );
    }

    [Fact]
    public async Task ClaimNextAvailableJobAsync_HandlesJobBeingClaimedByOtherConcurrentInvocation()
    {
        var custodianId = $"custodian-{Guid.NewGuid()}";

        await UpsertJobsAsync(
            new Job
            {
                JobId = $"job-{custodianId}",
                WorkItemId = $"wi-{custodianId}",
                CustodianId = custodianId,
                JobType = default,
                PayloadJson = "",
            }
        );

        var mockJobRepository = Substitute.For<IJobRepository>();

        mockJobRepository
            .ListJobsByCustodianIdAsync(
                custodianId,
                Arg.Any<DateTimeOffset>(),
                Arg.Any<CancellationToken>()
            )
            .Returns(async callInfo =>
            {
                var windowStart = callInfo.Arg<DateTimeOffset>();

                return await _jobRepository.ListJobsByCustodianIdAsync(
                    custodianId,
                    windowStart,
                    CancellationToken.None
                );
            });

        mockJobRepository
            .UpdateAsync(Arg.Any<Job>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(async callInfo =>
            {
                var job = callInfo.Arg<Job>();
                var ifMatchETag = callInfo.Arg<string>();

                // Simulate the job being claimed by a different invocation concurrently
                await _jobRepository.UpdateAsync(
                    job with
                    {
                        LeaseId = Guid.NewGuid().ToString(),
                    },
                    ifMatchETag,
                    CancellationToken.None
                );

                // Now do what would be our actual update to the job, which should now fail because the ETag is out of date
                await _jobRepository.UpdateAsync(job, ifMatchETag, CancellationToken.None);
            });

        _sut = new JobClaimService(
            mockJobRepository,
            _mockJobWindowStartService,
            _mockOptions,
            _mockTimeProvider,
            _mockLogger
        );

        // ACT
        var result = await _sut.ClaimNextAvailableJobAsync(custodianId);

        // ASSERT
        result.Should().BeNull();

        await mockJobRepository.Received(1).UpdateAsync(Arg.Any<Job>(), Arg.Any<string>());

        _mockLogger
            .Received(1)
            .Log(
                LogLevel.Warning,
                0,
                Arg.Is<object>(x =>
                    $"{x}".Equals($"Job job-{custodianId} was already claimed concurrently")
                ),
                Arg.Any<RequestFailedException>(),
                Arg.Any<Func<object, Exception?, string>>()
            );

        // Because the first job was claimed by another invocation, our invocation should have rescanned for a second time
        await mockJobRepository
            .Received(2)
            .ListJobsByCustodianIdAsync(
                custodianId,
                Arg.Any<DateTimeOffset>(),
                Arg.Any<CancellationToken>()
            );
    }

    [Fact]
    public async Task ClaimNextAvailableJobAsync_WouldClaimSecondJobIfIrstJobIsClaimedByOtherConcurrentInvocation()
    {
        var custodianId = $"custodian-{Guid.NewGuid()}";

        await UpsertJobsAsync(
            new Job
            {
                CreatedAtUtc = _mockJobWindowStartService.GetWindowStart().AddMinutes(1),
                JobId = $"job-1-{custodianId}",
                WorkItemId = $"wi-{custodianId}",
                CustodianId = custodianId,
                JobType = default,
                PayloadJson = "",
            },
            new Job
            {
                CreatedAtUtc = _mockJobWindowStartService.GetWindowStart().AddMinutes(2),
                JobId = $"job-2-{custodianId}",
                WorkItemId = $"wi-{custodianId}",
                CustodianId = custodianId,
                JobType = default,
                PayloadJson = "",
            }
        );

        var mockJobRepository = Substitute.For<IJobRepository>();

        mockJobRepository
            .ListJobsByCustodianIdAsync(
                custodianId,
                Arg.Any<DateTimeOffset>(),
                Arg.Any<CancellationToken>()
            )
            .Returns(async callInfo =>
            {
                var windowStart = callInfo.Arg<DateTimeOffset>();

                return await _jobRepository.ListJobsByCustodianIdAsync(
                    custodianId,
                    windowStart,
                    CancellationToken.None
                );
            });

        mockJobRepository
            .UpdateAsync(Arg.Any<Job>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(async callInfo =>
            {
                var job = callInfo.Arg<Job>();
                var ifMatchETag = callInfo.Arg<string>();

                // Simulate the job being claimed by a different invocation concurrently
                if (job.JobId.StartsWith("job-1"))
                {
                    await _jobRepository.UpdateAsync(
                        job with
                        {
                            LeaseId = Guid.NewGuid().ToString(),
                        },
                        ifMatchETag,
                        CancellationToken.None
                    );
                }

                // Now do what would be our actual update to the job, which should now fail because the ETag is out of date
                await _jobRepository.UpdateAsync(job, ifMatchETag, CancellationToken.None);
            });

        _sut = new JobClaimService(
            mockJobRepository,
            _mockJobWindowStartService,
            _mockOptions,
            _mockTimeProvider,
            _mockLogger
        );

        // ACT
        var result = await _sut.ClaimNextAvailableJobAsync(custodianId);

        // ASSERT
        result.Should().NotBeNull();
        result.JobId.Should().Be($"job-2-{custodianId}");

        await mockJobRepository.Received(2).UpdateAsync(Arg.Any<Job>(), Arg.Any<string>());

        _mockLogger
            .Received(1)
            .Log(
                LogLevel.Warning,
                0,
                Arg.Is<object>(x =>
                    $"{x}".Equals($"Job job-1-{custodianId} was already claimed concurrently")
                ),
                Arg.Any<RequestFailedException>(),
                Arg.Any<Func<object, Exception?, string>>()
            );
    }

    [Fact]
    public async Task ClaimNextAvailableJobAsync_ReScansOnlyMaximumNumberOfTimesWhenOtherJobsAreClaimedByOtherInvocationsConcurrently()
    {
        _mockOptions.CurrentValue.Returns(new JobClaimConfig { MaxReScanAttempts = 1 });

        var custodianId = $"custodian-{Guid.NewGuid()}";

        await UpsertJobsAsync(
            new Job
            {
                CreatedAtUtc = _mockJobWindowStartService.GetWindowStart().AddMinutes(1),
                JobId = $"job-1-{custodianId}",
                WorkItemId = $"wi-{custodianId}",
                CustodianId = custodianId,
                JobType = default,
                PayloadJson = "",
            },
            new Job
            {
                CreatedAtUtc = _mockJobWindowStartService.GetWindowStart().AddMinutes(2),
                JobId = $"job-2-{custodianId}",
                WorkItemId = $"wi-{custodianId}",
                CustodianId = custodianId,
                JobType = default,
                PayloadJson = "",
            },
            new Job
            {
                CreatedAtUtc = _mockJobWindowStartService.GetWindowStart().AddMinutes(3),
                JobId = $"job-3-{custodianId}",
                WorkItemId = $"wi-{custodianId}",
                CustodianId = custodianId,
                JobType = default,
                PayloadJson = "",
            }
        );

        var mockJobRepository = Substitute.For<IJobRepository>();

        mockJobRepository
            .ListJobsByCustodianIdAsync(
                custodianId,
                Arg.Any<DateTimeOffset>(),
                Arg.Any<CancellationToken>()
            )
            .Returns(async callInfo =>
            {
                var windowStart = callInfo.Arg<DateTimeOffset>();

                return await _jobRepository.ListJobsByCustodianIdAsync(
                    custodianId,
                    windowStart,
                    CancellationToken.None
                );
            });

        mockJobRepository
            .UpdateAsync(Arg.Any<Job>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(async callInfo =>
            {
                var job = callInfo.Arg<Job>();
                var ifMatchETag = callInfo.Arg<string>();

                // Simulate the job being claimed by a different invocation concurrently
                await _jobRepository.UpdateAsync(
                    job with
                    {
                        LeaseId = Guid.NewGuid().ToString(),
                    },
                    ifMatchETag,
                    CancellationToken.None
                );

                // Now do what would be our actual update to the job, which should now fail because the ETag is out of date
                await _jobRepository.UpdateAsync(job, ifMatchETag, CancellationToken.None);
            });

        _sut = new JobClaimService(
            mockJobRepository,
            _mockJobWindowStartService,
            _mockOptions,
            _mockTimeProvider,
            _mockLogger
        );

        // ACT
        var result = await _sut.ClaimNextAvailableJobAsync(custodianId);

        // ASSERT
        result.Should().BeNull();

        await mockJobRepository
            .Received(2)
            .ListJobsByCustodianIdAsync(
                custodianId,
                Arg.Any<DateTimeOffset>(),
                Arg.Any<CancellationToken>()
            );

        _mockLogger
            .Received(2)
            .Log(
                LogLevel.Warning,
                0,
                Arg.Is<object>(x => $"{x}".EndsWith("was already claimed concurrently")),
                Arg.Any<RequestFailedException>(),
                Arg.Any<Func<object, Exception?, string>>()
            );
    }

    [Fact]
    public async Task ClaimNextAvailableJobAsync_UsesNullSuiAndRecordType_WhenJobTypeIsNot_CustodianLookup()
    {
        var custodianId = $"custodian-{Guid.NewGuid()}";

        await UpsertJobsAsync(
            new Job
            {
                JobId = $"job-{custodianId}",
                WorkItemId = $"wi-{custodianId}",
                CustodianId = custodianId,
                JobType = JobType.Unknown,
                PayloadJson = "",
            }
        );

        // ACT
        var result = await _sut.ClaimNextAvailableJobAsync(custodianId);

        // ASSERT
        result.Should().NotBeNull();
        result.JobId.Should().Be($"job-{custodianId}");
        result.Sui.Should().BeNull();
        result.RecordType.Should().BeNull();
    }
}
