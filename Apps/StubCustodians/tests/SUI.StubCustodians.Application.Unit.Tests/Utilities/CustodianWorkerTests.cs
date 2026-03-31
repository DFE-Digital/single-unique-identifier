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
    private readonly IBaseUrlProvider _baseUrlProvider = Substitute.For<IBaseUrlProvider>();
    private readonly IManifestService _manifestService = Substitute.For<IManifestService>();
    private readonly IServiceProvider _serviceProvider = Substitute.For<IServiceProvider>();
    private readonly IServiceScope _serviceScope = Substitute.For<IServiceScope>();
    private readonly IServiceScopeFactory _scopeFactory = Substitute.For<IServiceScopeFactory>();

    private readonly Organisation _testOrg;

    public CustodianWorkerTests()
    {
        _testOrg = new Organisation
        {
            OrgId = "test-org-123",
            Records =
            [
                new RecordDefinition
                {
                    RecordType = "RT-1",
                    Connection = new Connection
                    {
                        Auth = new AuthConfig() { ClientId = "client-id", ClientSecret = "secret" },
                    },
                },
            ],
        };

        _logger.IsEnabled(LogLevel.Information).Returns(true);

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
        _baseUrlProvider.GetBaseUrl().Returns("https://api.test");

        _manifestService
            .GetManifestForOrganisation(
                _testOrg.OrgId,
                job.Sui,
                "https://api.test",
                job.RecordType,
                Arg.Any<CancellationToken>()
            )
            .Returns(manifestItems);

        // Act
        var worker = new CustodianWorker(
            _logger,
            _tokenProvider,
            _client,
            _baseUrlProvider,
            _testOrg,
            _serviceProvider
        );
        using var cts = new CancellationTokenSource();

        var task = worker.StartAsync(cts.Token);
        await Task.Delay(100, cts.Token); // Allow the worker loop to run at least once
        await cts.CancelAsync();
        await task;

        // Assert
        await _client
            .Received() // There is a slight possibility of execution speed where the count can vary
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

        // Act
        var worker = new CustodianWorker(
            _logger,
            _tokenProvider,
            _client,
            _baseUrlProvider,
            _testOrg,
            _serviceProvider
        );
        using var cts = new CancellationTokenSource();

        var task = worker.StartAsync(cts.Token);
        await Task.Delay(100, cts.Token);
        await cts.CancelAsync();
        await task;

        // Assert
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

        // Act
        var worker = new CustodianWorker(
            _logger,
            _tokenProvider,
            _client,
            _baseUrlProvider,
            _testOrg,
            _serviceProvider
        );
        using var cts = new CancellationTokenSource();

        var task = worker.StartAsync(cts.Token);
        await Task.Delay(100, cts.Token);
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
}
