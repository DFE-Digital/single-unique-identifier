using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using SUI.StubCustodians.Application.Interfaces;
using SUI.StubCustodians.Application.Models;
using SUI.StubCustodians.Application.Utilities;

namespace SUI.StubCustodians.Application.Unit.Tests.Utilities;

public class CustodianWorkerTests
{
    private readonly ILogger<CustodianWorker> _logger = Substitute.For<ILogger<CustodianWorker>>();
    private readonly ITokenProvider _tokenProvider = Substitute.For<ITokenProvider>();
    private readonly IFindApiClient _client = Substitute.For<IFindApiClient>();
    private readonly IConfiguration _config = Substitute.For<IConfiguration>();
    private readonly IManifestService _manifestService = Substitute.For<IManifestService>();
    private readonly IServiceProvider _serviceProvider = Substitute.For<IServiceProvider>();
    private readonly IDelayService _delayService = Substitute.For<IDelayService>();
    private readonly IServiceScope _serviceScope = Substitute.For<IServiceScope>();
    private readonly IServiceScopeFactory _scopeFactory = Substitute.For<IServiceScopeFactory>();

    private readonly AuthClient _testClient;

    public CustodianWorkerTests()
    {
        _testClient = new AuthClient()
        {
            ClientId = "client-id",
            ClientSecret = "secret",
            Enabled = true,
            AllowedScopes = ["test-scope"],
        };

        _logger.IsEnabled(LogLevel.Information).Returns(true);

        // Ensure delay does not actually wait during tests
        _delayService
            .DelayAsync(Arg.Any<TimeSpan>(), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        // Setup Service Provider Mocking Chain
        _serviceProvider.GetService(typeof(IServiceScopeFactory)).Returns(_scopeFactory);
        _scopeFactory.CreateScope().Returns(_serviceScope);
        _serviceScope.ServiceProvider.Returns(_serviceProvider);
        _serviceProvider.GetService(typeof(IManifestService)).Returns(_manifestService);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldProcessJob_WhenJobIsAvailable()
    {
        // Arrange
        var token = "fake-jwt-token";

        var job = new JobInfo
        {
            JobId = "job-abc",
            LeaseId = "lease-123",
            CustodianId = "cust-1",
            LeaseExpiresAtUtc = DateTimeOffset.UtcNow.AddMinutes(5),
            Sui = "SUI-888",
            RecordType = "Document",
        };

        var manifestItems = new List<SearchResultItem>
        {
            new("Document", "http://file", "rec-1", "sys-1"),
        };

        _tokenProvider.GetTokenAsync("client-id", "secret").Returns(token);
        _client.ClaimAsync(token).Returns(job);
        _client
            .ExtendLeaseAsync(token, Arg.Any<RenewJobLeaseRequest>())
            .Returns(
                new RenewJobLeaseResponse
                {
                    JobId = job.JobId,
                    LeaseId = job.LeaseId,
                    WorkItemId = "work-1",
                    LeaseExpiresUtc = job.LeaseExpiresAtUtc.AddMinutes(1),
                }
            );
        _config["StubCustodians:BaseUrl"].Returns("https://api.test");

        _manifestService
            .GetManifestForOrganisation(
                _testClient.ClientId,
                job.Sui,
                "https://api.test",
                job.RecordType,
                Arg.Any<CancellationToken>()
            )
            .Returns(manifestItems);

        var worker = new CustodianWorker(
            _logger,
            _tokenProvider,
            _client,
            _config,
            _testClient,
            _serviceProvider,
            _delayService
        );

        using var cts = new CancellationTokenSource();

        // Act
        var task = worker.StartAsync(cts.Token);
        await Task.Delay(100);
        await cts.CancelAsync();
        await task;

        // Assert
        await _client
            .Received()
            .ExtendLeaseAsync(
                token,
                Arg.Is<RenewJobLeaseRequest>(r => r.JobId == job.JobId && r.LeaseId == job.LeaseId)
            );

        await _client
            .Received()
            .SubmitAsync(
                token,
                Arg.Is<SubmitJobResultsRequest>(r =>
                    r.JobId == "job-abc" && r.ResultType == "HasRecords" && r.Records.Count == 1
                )
            );
    }

    [Fact]
    public async Task ExecuteAsync_ShouldLogWarning_WhenJobMissingSui()
    {
        // Arrange
        var job = new JobInfo
        {
            JobId = "job-bad",
            LeaseId = "L",
            CustodianId = "C",
            LeaseExpiresAtUtc = DateTimeOffset.UtcNow,
            Sui = null,
        };

        _tokenProvider.GetTokenAsync(Arg.Any<string>(), Arg.Any<string>()).Returns("token");

        _client.ClaimAsync("token").Returns(job);
        _client
            .ExtendLeaseAsync("token", Arg.Any<RenewJobLeaseRequest>())
            .Returns(
                new RenewJobLeaseResponse
                {
                    JobId = job.JobId,
                    LeaseId = job.LeaseId,
                    WorkItemId = "W",
                    LeaseExpiresUtc = job.LeaseExpiresAtUtc.AddMinutes(1),
                }
            );

        var worker = new CustodianWorker(
            _logger,
            _tokenProvider,
            _client,
            _config,
            _testClient,
            _serviceProvider,
            _delayService
        );

        using var cts = new CancellationTokenSource();

        // Act
        var task = worker.StartAsync(cts.Token);
        await Task.Delay(100);
        await cts.CancelAsync();
        await task;

        // Assert
        await _client.Received().ExtendLeaseAsync("token", Arg.Any<RenewJobLeaseRequest>());
        _logger
            .Received()
            .Log(
                LogLevel.Warning,
                Arg.Any<EventId>(),
                Arg.Is<Arg.AnyType>((object x) => x.ToString()!.Contains("Job missing Sui")),
                null,
                Arg.Any<Func<Arg.AnyType, Exception?, string>>()
            );
    }

    [Fact]
    public async Task ExecuteAsync_ShouldHandleExceptions_WithoutCrashing()
    {
        // Arrange
        _tokenProvider
            .GetTokenAsync(Arg.Any<string>(), Arg.Any<string>())
            .Throws(new Exception("API Down"));

        var worker = new CustodianWorker(
            _logger,
            _tokenProvider,
            _client,
            _config,
            _testClient,
            _serviceProvider,
            _delayService
        );

        using var cts = new CancellationTokenSource();

        // Act
        var task = worker.StartAsync(cts.Token);
        await Task.Delay(100);
        await cts.CancelAsync();
        await task;

        // Assert
        _logger
            .Received()
            .Log(
                LogLevel.Error,
                Arg.Any<EventId>(),
                Arg.Is<Arg.AnyType>((object x) => x.ToString()!.Contains("Polling failed")),
                Arg.Is<Exception>(ex => ex.Message == "API Down"),
                Arg.Any<Func<Arg.AnyType, Exception?, string>>()
            );
    }

    [Fact]
    public async Task ExecuteAsync_ShouldSleep_WhenNoJobReturned()
    {
        // Arrange
        _tokenProvider.GetTokenAsync(Arg.Any<string>(), Arg.Any<string>()).Returns("token");

        _client.ClaimAsync("token").Returns((JobInfo?)null);

        var worker = new CustodianWorker(
            _logger,
            _tokenProvider,
            _client,
            _config,
            _testClient,
            _serviceProvider,
            _delayService
        );

        using var cts = new CancellationTokenSource();

        // Act
        var task = worker.StartAsync(cts.Token);
        await Task.Delay(100);
        await cts.CancelAsync();
        await task;

        // Assert
        await _delayService
            .Received()
            .DelayAsync(Arg.Any<TimeSpan>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteAsync_ShouldNotSleep_WhenJobIsReturned()
    {
        // Arrange
        var token = "token";

        var job = new JobInfo
        {
            JobId = "job-1",
            LeaseId = "lease-1",
            CustodianId = "cust-1",
            LeaseExpiresAtUtc = DateTimeOffset.UtcNow.AddMinutes(5),
            Sui = "SUI-123",
            RecordType = "Document",
        };

        _tokenProvider.GetTokenAsync(Arg.Any<string>(), Arg.Any<string>()).Returns(token);

        _client.ClaimAsync(token).Returns(job);
        _client
            .ExtendLeaseAsync(token, Arg.Any<RenewJobLeaseRequest>())
            .Returns(
                new RenewJobLeaseResponse
                {
                    JobId = job.JobId,
                    LeaseId = job.LeaseId,
                    WorkItemId = "W",
                    LeaseExpiresUtc = job.LeaseExpiresAtUtc.AddMinutes(1),
                }
            );

        _config["StubCustodians:BaseUrl"].Returns("https://api.test");

        _manifestService
            .GetManifestForOrganisation(
                _testClient.ClientId,
                job.Sui,
                "https://api.test",
                job.RecordType,
                Arg.Any<CancellationToken>()
            )
            .Returns(new List<SearchResultItem>());

        var worker = new CustodianWorker(
            _logger,
            _tokenProvider,
            _client,
            _config,
            _testClient,
            _serviceProvider,
            _delayService
        );

        using var cts = new CancellationTokenSource();

        // Act
        var task = worker.StartAsync(cts.Token);
        await Task.Delay(100);
        await cts.CancelAsync();
        await task;

        // Assert
        await _client.Received().ExtendLeaseAsync(token, Arg.Any<RenewJobLeaseRequest>());
        await _delayService
            .DidNotReceive()
            .DelayAsync(Arg.Any<TimeSpan>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteAsync_ShouldLogLeaseExtended_WhenLeaseIsSuccessfullyExtended()
    {
        // Arrange
        var token = "token";
        var job = new JobInfo
        {
            JobId = "job-1",
            LeaseId = "lease-1",
            CustodianId = "cust-1",
            LeaseExpiresAtUtc = DateTimeOffset.UtcNow,
            Sui = "SUI-123",
        };
        var extendedLease = new RenewJobLeaseResponse
        {
            JobId = job.JobId,
            LeaseId = job.LeaseId,
            WorkItemId = "W",
            LeaseExpiresUtc = job.LeaseExpiresAtUtc.AddMinutes(1),
        };

        _tokenProvider.GetTokenAsync(Arg.Any<string>(), Arg.Any<string>()).Returns(token);
        _client.ClaimAsync(token).Returns(job);
        _client.ExtendLeaseAsync(token, Arg.Any<RenewJobLeaseRequest>()).Returns(extendedLease);

        var worker = new CustodianWorker(
            _logger,
            _tokenProvider,
            _client,
            _config,
            _testClient,
            _serviceProvider,
            _delayService
        );

        using var cts = new CancellationTokenSource();

        // Act
        var task = worker.StartAsync(cts.Token);
        await Task.Delay(100);
        await cts.CancelAsync();
        await task;

        // Assert
        _logger
            .Received()
            .Log(
                LogLevel.Information,
                Arg.Any<EventId>(),
                Arg.Is<Arg.AnyType>((object v) => v.ToString()!.Contains("Lease extended for job")),
                null,
                Arg.Any<Func<Arg.AnyType, Exception?, string>>()
            );
    }

    [Fact]
    public async Task ExecuteAsync_ShouldLogLeaseNotExtended_WhenLeaseExpirationIsNotIncreased()
    {
        // Arrange
        var token = "token";
        var job = new JobInfo
        {
            JobId = "job-1",
            LeaseId = "lease-1",
            CustodianId = "cust-1",
            LeaseExpiresAtUtc = DateTimeOffset.UtcNow.AddMinutes(5),
            Sui = "SUI-123",
        };
        var extendedLease = new RenewJobLeaseResponse
        {
            JobId = job.JobId,
            LeaseId = job.LeaseId,
            WorkItemId = "W",
            LeaseExpiresUtc = job.LeaseExpiresAtUtc, // Not increased
        };

        _tokenProvider.GetTokenAsync(Arg.Any<string>(), Arg.Any<string>()).Returns(token);
        _client.ClaimAsync(token).Returns(job);
        _client.ExtendLeaseAsync(token, Arg.Any<RenewJobLeaseRequest>()).Returns(extendedLease);

        var worker = new CustodianWorker(
            _logger,
            _tokenProvider,
            _client,
            _config,
            _testClient,
            _serviceProvider,
            _delayService
        );

        using var cts = new CancellationTokenSource();

        // Act
        var task = worker.StartAsync(cts.Token);
        await Task.Delay(100);
        await cts.CancelAsync();
        await task;

        // Assert
        _logger
            .Received()
            .Log(
                LogLevel.Information,
                Arg.Any<EventId>(),
                Arg.Is<Arg.AnyType>(
                    (object v) => v.ToString()!.Contains("Lease not extended for job")
                ),
                null,
                Arg.Any<Func<Arg.AnyType, Exception?, string>>()
            );
    }
}
