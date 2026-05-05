using System.Text.Json;
using Azure;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;
using SUI.Find.Application.Dtos;
using SUI.Find.Application.Enums;
using SUI.Find.Application.Interfaces;
using SUI.Find.Application.Models;
using SUI.Find.Infrastructure.Configuration;
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
        var result = await _sut.ClaimNextAvailableJobAsync(custodianId, CancellationToken.None);

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
                    RequestingOrganisationId = $"RequestingOrganisation_{Guid.NewGuid()}",
                    JobType = default,
                    PayloadJson = "",
                })
        );

        // ACT
        var result = await _sut.ClaimNextAvailableJobAsync(custodianId, CancellationToken.None);

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
                    RequestingOrganisationId = $"RequestingOrganisation_{Guid.NewGuid()}",
                    CustodianId = custodianId,
                    JobType = default,
                    PayloadJson = "",
                })
        );

        // ACT
        var result = await _sut.ClaimNextAvailableJobAsync(custodianId, CancellationToken.None);

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
                    RequestingOrganisationId = $"RequestingOrganisation_{Guid.NewGuid()}",
                    CustodianId = custodianId,
                    JobType = default,
                    PayloadJson = "",
                })
        );

        // ACT
        var result = await _sut.ClaimNextAvailableJobAsync(custodianId, CancellationToken.None);

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
                    RequestingOrganisationId = $"RequestingOrganisation_{Guid.NewGuid()}",
                    CustodianId = custodianId,
                    JobType = default,
                    PayloadJson = "",
                })
        );

        // ACT
        var result = await _sut.ClaimNextAvailableJobAsync(custodianId, CancellationToken.None);

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
                RequestingOrganisationId = $"RequestingOrganisation_{Guid.NewGuid()}",
                JobType = default,
                PayloadJson = "",
            },
            new Job
            {
                CreatedAtUtc = _mockJobWindowStartService.GetWindowStart().AddHours(0.5), // This job should be chosen because it has the earliest creation date
                JobId = $"job-x-{custodianId}",
                WorkItemId = $"wi-{custodianId}",
                CustodianId = custodianId,
                RequestingOrganisationId = $"RequestingOrganisation_{Guid.NewGuid()}",
                JobType = default,
                PayloadJson = "",
            }
        );

        // ACT
        var result = await _sut.ClaimNextAvailableJobAsync(custodianId, CancellationToken.None);

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
                RequestingOrganisationId = $"RequestingOrganisation_{Guid.NewGuid()}",
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
        var result = await _sut.ClaimNextAvailableJobAsync(custodianId, CancellationToken.None);

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
    public async Task ClaimNextAvailableJobAsync_WouldClaimSecondJobIfFirstJobIsClaimedByOtherConcurrentInvocation()
    {
        var custodianId = $"custodian-{Guid.NewGuid()}";

        await UpsertJobsAsync(
            new Job
            {
                CreatedAtUtc = _mockJobWindowStartService.GetWindowStart().AddMinutes(1),
                JobId = $"job-1-{custodianId}",
                WorkItemId = $"wi-{custodianId}",
                CustodianId = custodianId,
                RequestingOrganisationId = $"RequestingOrganisation_{Guid.NewGuid()}",
                JobType = default,
                PayloadJson = "",
            },
            new Job
            {
                CreatedAtUtc = _mockJobWindowStartService.GetWindowStart().AddMinutes(2),
                JobId = $"job-2-{custodianId}",
                WorkItemId = $"wi-{custodianId}",
                CustodianId = custodianId,
                RequestingOrganisationId = $"RequestingOrganisation_{Guid.NewGuid()}",
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
        var result = await _sut.ClaimNextAvailableJobAsync(custodianId, CancellationToken.None);

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
                RequestingOrganisationId = $"RequestingOrganisation_{Guid.NewGuid()}",
                JobType = default,
                PayloadJson = "",
            },
            new Job
            {
                CreatedAtUtc = _mockJobWindowStartService.GetWindowStart().AddMinutes(2),
                JobId = $"job-2-{custodianId}",
                WorkItemId = $"wi-{custodianId}",
                CustodianId = custodianId,
                RequestingOrganisationId = $"RequestingOrganisation_{Guid.NewGuid()}",
                JobType = default,
                PayloadJson = "",
            },
            new Job
            {
                CreatedAtUtc = _mockJobWindowStartService.GetWindowStart().AddMinutes(3),
                JobId = $"job-3-{custodianId}",
                WorkItemId = $"wi-{custodianId}",
                CustodianId = custodianId,
                RequestingOrganisationId = $"RequestingOrganisation_{Guid.NewGuid()}",
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
        var result = await _sut.ClaimNextAvailableJobAsync(custodianId, CancellationToken.None);

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
                RequestingOrganisationId = $"RequestingOrganisation_{Guid.NewGuid()}",
                JobType = JobType.Unknown,
                PayloadJson = "",
            }
        );

        // ACT
        var result = await _sut.ClaimNextAvailableJobAsync(custodianId, CancellationToken.None);

        // ASSERT
        result.Should().NotBeNull();
        result.JobId.Should().Be($"job-{custodianId}");
        result.Sui.Should().BeNull();
        result.RecordType.Should().BeNull();
    }

    [Fact]
    public async Task DoesCustodianHaveJobs_ReturnsTrue_When_JobsAreAvailable()
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
                RequestingOrganisationId = $"RequestingOrganisation_{Guid.NewGuid()}",
                JobType = default,
                PayloadJson = "",
            }
        );

        // ACT
        var result = await _sut.DoesCustodianHaveJobs(custodianId, CancellationToken.None);

        // ASSERT
        result.Should().BeTrue();
    }

    [Fact]
    public async Task DoesCustodianHaveJobs_ReturnsFalse_When_NoJobsAreAvailable()
    {
        var custodianId = $"custodian-{Guid.NewGuid()}";

        // ACT
        var result = await _sut.DoesCustodianHaveJobs(custodianId, CancellationToken.None);

        // ASSERT
        result.Should().BeFalse();
    }

    [Fact]
    public async Task ClaimNextAvailableJobAsync_ExtractsSuiAndRecordTypeFromPayload_WhenJobTypeIs_CustodianLookup()
    {
        var custodianId = $"custodian-{Guid.NewGuid()}";

        await UpsertJobsAsync(
            new Job
            {
                JobId = $"job-{custodianId}",
                WorkItemId = $"wi-{custodianId}",
                CustodianId = custodianId,
                RequestingOrganisationId = $"RequestingOrganisation_{Guid.NewGuid()}",
                JobType = JobType.CustodianLookup,
                PayloadJson = JsonSerializer.Serialize(
                    new CustodianLookupJobPayload("example-sui-123ABC", "example-record.type.xyz")
                ),
            }
        );

        // ACT
        var result = await _sut.ClaimNextAvailableJobAsync(custodianId, CancellationToken.None);

        // ASSERT
        result.Should().NotBeNull();
        result.JobId.Should().Be($"job-{custodianId}");
        result.Sui.Should().Be("example-sui-123ABC");
        result.RecordType.Should().Be("example-record.type.xyz");
    }

    [Fact]
    public async Task ClaimNextAvailableJobAsync_WouldClaimPreviouslyLeasedJob_IfLeaseHasNowExpired()
    {
        var custodianId = $"custodian-{Guid.NewGuid()}";

        _mockTimeProvider.GetUtcNow().Returns(DateTimeOffset.UtcNow);
        _mockJobWindowStartService.GetWindowStart().Returns(DateTimeOffset.UtcNow.AddHours(-10));

        var oldLeaseId = Guid.NewGuid().ToString();

        await UpsertJobsAsync(
            new Job
            {
                CreatedAtUtc = _mockJobWindowStartService.GetWindowStart().AddHours(1),
                LeaseId = oldLeaseId,
                LeaseExpiresAtUtc = _mockTimeProvider.GetUtcNow().AddMinutes(-1),
                JobId = $"job-{custodianId}",
                WorkItemId = $"wi-{custodianId}",
                CustodianId = custodianId,
                RequestingOrganisationId = $"RequestingOrganisation_{Guid.NewGuid()}",
                JobType = JobType.CustodianLookup,
                PayloadJson = JsonSerializer.Serialize(
                    new CustodianLookupJobPayload("example-sui-123ABC", "example-record.type.xyz")
                ),
            }
        );

        // ACT
        var result = await _sut.ClaimNextAvailableJobAsync(custodianId, CancellationToken.None);

        // ASSERT
        result.Should().NotBeNull();
        result.JobId.Should().Be($"job-{custodianId}");
        result.LeaseId.Should().NotBe(oldLeaseId).And.NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task ExtendJobLeaseAsync_ReturnsNull_When_NoJobFound()
    {
        var custodianId = $"custodian-{Guid.NewGuid()}";
        var jobId = $"job-{Guid.NewGuid()}";
        var leaseId = Guid.NewGuid().ToString();

        // ACT
        var result = await _sut.ExtendJobLeaseAsync(
            custodianId,
            jobId,
            leaseId,
            CancellationToken.None
        );

        // ASSERT
        result.Should().BeNull();
    }

    [Fact]
    public async Task ExtendJobLeaseAsync_ReturnsNull_When_JobHasNoLease()
    {
        var custodianId = $"custodian-{Guid.NewGuid()}";
        var jobId = $"job-{custodianId}";
        var workItemId = $"wi-{Guid.NewGuid()}";
        var leaseId = Guid.NewGuid().ToString();

        _mockJobWindowStartService.GetWindowStart().Returns(DateTimeOffset.UtcNow.AddHours(-10));

        await UpsertJobsAsync(
            new Job
            {
                JobId = jobId,
                WorkItemId = workItemId,
                CustodianId = custodianId,
                RequestingOrganisationId = $"RequestingOrganisation_{Guid.NewGuid()}",
                JobType = default,
                PayloadJson = "",
                LeaseExpiresAtUtc = null, // No lease
                CreatedAtUtc = DateTimeOffset.UtcNow.AddMinutes(-5),
            }
        );

        // ACT
        var result = await _sut.ExtendJobLeaseAsync(
            custodianId,
            jobId,
            leaseId,
            CancellationToken.None
        );

        // ASSERT
        result.Should().BeNull();
    }

    [Fact]
    public async Task ExtendJobLeaseAsync_ExtendsLease_When_JobIsFound()
    {
        var custodianId = $"custodian-{Guid.NewGuid()}";
        var jobId = $"job-{custodianId}";
        var workItemId = $"wi-{Guid.NewGuid()}";
        var leaseId = Guid.NewGuid().ToString();
        var initialLeaseExpiresAtUtc = DateTimeOffset.UtcNow.AddMinutes(10);

        _mockJobWindowStartService.GetWindowStart().Returns(DateTimeOffset.UtcNow.AddHours(-10));
        _mockTimeProvider.GetUtcNow().Returns(DateTimeOffset.UtcNow);

        await UpsertJobsAsync(
            new Job
            {
                JobId = jobId,
                WorkItemId = workItemId,
                CustodianId = custodianId,
                RequestingOrganisationId = $"RequestingOrganisation_{Guid.NewGuid()}",
                JobType = default,
                PayloadJson = "",
                LeaseId = leaseId,
                LeaseExpiresAtUtc = initialLeaseExpiresAtUtc,
                CreatedAtUtc = DateTimeOffset.UtcNow.AddMinutes(-5),
            }
        );

        // ACT
        var result = await _sut.ExtendJobLeaseAsync(
            custodianId,
            jobId,
            leaseId,
            CancellationToken.None
        );

        // ASSERT
        result.Should().NotBeNull();
        result.LeaseId.Should().Be(leaseId);
        result
            .LeaseExpiresAtUtc.Should()
            .Be(
                initialLeaseExpiresAtUtc.AddMinutes(_mockOptions.CurrentValue.LeaseDurationMinutes)
            );

        var dbJobs = await _jobRepository.ListJobsByCustodianIdAsync(
            custodianId,
            DateTimeOffset.MinValue
        );
        var job = dbJobs.Single(j => j.WorkItemId == workItemId);
        job.LeaseExpiresAtUtc.Should().Be(result.LeaseExpiresAtUtc);
    }
}
