using NSubstitute;
using SUI.Find.Application.Enums;
using SUI.Find.Application.Interfaces;
using SUI.Find.Infrastructure.Repositories.JobRepository;
using SUI.Find.Infrastructure.Services;

namespace SUI.Find.Infrastructure.UnitTests.Services
{
    public class JobProcessorServiceTests
    {
        private readonly IJobRepository _jobRepository = Substitute.For<IJobRepository>();
        private readonly IJobWindowStartService _jobWindowStartService =
            Substitute.For<IJobWindowStartService>();
        private readonly JobProcessorService _service;

        public JobProcessorServiceTests()
        {
            _jobWindowStartService.GetWindowStart().Returns(DateTimeOffset.UtcNow.AddDays(-1));
            _service = new JobProcessorService(_jobRepository, _jobWindowStartService);
        }

        private Job CreateJob(
            string? jobId = null,
            string? leaseId = "lease1",
            string? custodianId = "cust1",
            DateTimeOffset? leaseExpires = null,
            DateTimeOffset? completedAt = null
        )
        {
            return new Job
            {
                JobId = jobId ?? Guid.NewGuid().ToString(),
                SearchingOrganisationId = "search-org-1",
                LeaseId = leaseId,
                CustodianId = custodianId!,
                LeaseExpiresAtUtc = leaseExpires ?? DateTimeOffset.UtcNow.AddMinutes(5),
                CompletedAtUtc = completedAt,
                JobType = JobType.CustodianLookup,
                PayloadJson = "{}",
            };
        }

        private void SetupRepo(params Job[] jobs)
        {
            _jobRepository
                .ListJobsByCustodianIdAsync(
                    Arg.Any<string>(),
                    Arg.Any<DateTimeOffset>(),
                    Arg.Any<CancellationToken>()
                )
                .Returns(jobs.ToList());
        }

        #region ValidateLeaseAsync Tests

        [Fact]
        public async Task ValidateLeaseAsync_ShouldReturnNull_WhenJobNotFound()
        {
            SetupRepo(); // no jobs
            var result = await _service.ValidateLeaseAsync(
                "missing",
                "lease",
                "cust1",
                CancellationToken.None
            );
            Assert.Null(result);
        }

        [Fact]
        public async Task ValidateLeaseAsync_ShouldReturnNull_WhenLeaseIdIsNull()
        {
            var job = CreateJob(leaseId: null);
            SetupRepo(job);

            var result = await _service.ValidateLeaseAsync(
                job.JobId,
                "lease1",
                job.CustodianId,
                CancellationToken.None
            );
            Assert.Null(result);
        }

        [Fact]
        public async Task ValidateLeaseAsync_ShouldReturnNull_WhenLeaseIdMismatch()
        {
            var job = CreateJob(leaseId: "correct");
            SetupRepo(job);

            var result = await _service.ValidateLeaseAsync(
                job.JobId,
                "wrong",
                job.CustodianId,
                CancellationToken.None
            );
            Assert.Null(result);
        }

        [Fact]
        public async Task ValidateLeaseAsync_ShouldReturnNull_WhenLeaseExpired()
        {
            var job = CreateJob(leaseExpires: DateTimeOffset.UtcNow.AddSeconds(-1));
            SetupRepo(job);

            var result = await _service.ValidateLeaseAsync(
                job.JobId,
                job.LeaseId!,
                job.CustodianId,
                CancellationToken.None
            );
            Assert.Null(result);
        }

        [Fact]
        public async Task ValidateLeaseAsync_ShouldReturnJob_WhenValid()
        {
            var job = CreateJob(leaseExpires: DateTimeOffset.UtcNow.AddMinutes(5));
            SetupRepo(job);

            var result = await _service.ValidateLeaseAsync(
                job.JobId,
                job.LeaseId!,
                job.CustodianId,
                CancellationToken.None
            );
            Assert.NotNull(result);
            Assert.Equal(job.JobId, result.JobId);
            Assert.Equal(job.LeaseId, result.LeaseId);
            Assert.Equal(job.CustodianId, result.CustodianId);
        }

        #endregion

        #region GetJobByIdAndCustodianIdAsync Tests

        [Fact]
        public async Task GetJobByIdAndCustodianIdAsync_ShouldReturnJob_WhenFound()
        {
            var job = CreateJob();
            SetupRepo(job);

            var result = await _service.GetJobByIdAndCustodianIdAsync(
                job.JobId,
                job.CustodianId,
                CancellationToken.None
            );
            Assert.NotNull(result);
            Assert.Equal(job.JobId, result.JobId);
        }

        [Fact]
        public async Task GetJobByIdAndCustodianIdAsync_ShouldReturnNull_WhenNotFound()
        {
            SetupRepo(); // empty
            var result = await _service.GetJobByIdAndCustodianIdAsync(
                "missing",
                "cust1",
                CancellationToken.None
            );
            Assert.Null(result);
        }

        #endregion

        #region MarkCompletedAsync Tests

        [Fact]
        public async Task MarkCompletedAsync_ShouldSetCompletedAtUtc_WhenJobExists()
        {
            var job = CreateJob();
            SetupRepo(job);

            await _service.MarkCompletedAsync(job.JobId, job.CustodianId, CancellationToken.None);

            await _jobRepository
                .Received(1)
                .UpsertAsync(
                    Arg.Is<Job>(j => j.JobId == job.JobId && j.CompletedAtUtc.HasValue),
                    Arg.Any<CancellationToken>()
                );
        }

        [Fact]
        public async Task MarkCompletedAsync_ShouldNotThrow_WhenJobDoesNotExist()
        {
            SetupRepo(); // empty

            var ex = await Record.ExceptionAsync(() =>
                _service.MarkCompletedAsync("missing", "cust1", CancellationToken.None)
            );

            Assert.Null(ex);
        }

        #endregion
    }
}
