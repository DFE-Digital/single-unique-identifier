using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SUI.StubCustodians.Application.Interfaces;
using SUI.StubCustodians.Application.Models;

namespace SUI.StubCustodians.Application.Utilities;

public class CustodianWorker : BackgroundService
{
    private readonly ILogger<CustodianWorker> _logger;
    private readonly ITokenProvider _tokenProvider;
    private readonly IFindApiClient _findApiClient;
    private readonly IConfiguration _config;
    private readonly AuthClient _authClient;
    private readonly IServiceProvider _services;
    private readonly IDelayService _delayService;

    private readonly string _clientId;
    private readonly string _clientSecret;
    private readonly int _intervalSeconds;

    public CustodianWorker(
        ILogger<CustodianWorker> logger,
        ITokenProvider tokenProvider,
        IFindApiClient findApiClient,
        IConfiguration config,
        AuthClient authClient,
        IServiceProvider services,
        IDelayService delayService
    )
    {
        _logger = logger;
        _tokenProvider = tokenProvider;
        _findApiClient = findApiClient;
        _config = config;
        _authClient = authClient;
        _services = services;
        _delayService = delayService;

        _clientId = authClient.ClientId;
        _clientSecret = authClient.ClientSecret;

        _intervalSeconds = Random.Shared.Next(30, 121);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (_logger.IsEnabled(LogLevel.Information))
            _logger.LogInformation(
                "Custodian {ClientId} started. Interval: {Interval}s",
                _authClient.ClientId,
                _intervalSeconds
            );

        while (!stoppingToken.IsCancellationRequested)
        {
            bool sleep;

            try
            {
                var token = await _tokenProvider.GetTokenAsync(_clientId, _clientSecret);

                using var scope = _services.CreateScope();
                var manifestService = scope.ServiceProvider.GetRequiredService<IManifestService>();

                var job = await _findApiClient.ClaimAsync(token);

                sleep = job == null; // i.e. do not sleep if we have just claimed a job, because there may be more to claim straight away. Only sleep if there was nothing to claim.

                if (job != null)
                {
                    var extendedLease = await _findApiClient.ExtendLeaseAsync(
                        token,
                        new RenewJobLeaseRequest { JobId = job.JobId, LeaseId = job.LeaseId }
                    );
                    if (_logger.IsEnabled(LogLevel.Information))
                    {
                        _logger.LogInformation(
                            extendedLease?.LeaseExpiresUtc > job.LeaseExpiresAtUtc
                                ? "Lease extended for job: {JobId}"
                                : "Lease not extended for job: {JobId}",
                            extendedLease?.JobId
                        );
                    }

                    await ProcessJob(job, token, manifestService, stoppingToken);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Polling failed for {ClientId}", _authClient.ClientId);
                sleep = true;
            }

            if (sleep)
            {
                await _delayService.DelayAsync(
                    TimeSpan.FromSeconds(_intervalSeconds),
                    stoppingToken
                );
            }
        }
    }

    private async Task ProcessJob(
        JobInfo job,
        string token,
        IManifestService manifestService,
        CancellationToken ct
    )
    {
        using var scope = _logger.BeginScope(
            new Dictionary<string, object>
            {
                ["JobId"] = job.JobId,
                ["LeaseId"] = job.LeaseId,
                ["OrgId"] = _authClient.ClientId,
            }
        );

        if (_logger.IsEnabled(LogLevel.Information))
            _logger.LogInformation(
                "Processing job {JobDetails}",
                new
                {
                    job.JobId,
                    job.LeaseId,
                    _authClient.ClientId,
                }
            );

        if (string.IsNullOrWhiteSpace(job.Sui))
        {
            _logger.LogWarning("Job missing Sui");
            return;
        }

        var baseUrl = _config["StubCustodians:BaseUrl"]!;

        var manifest = await manifestService.GetManifestForOrganisation(
            _authClient.ClientId,
            job.Sui,
            baseUrl,
            job.RecordType,
            ct
        );

        var result = new SubmitJobResultsRequest
        {
            JobId = job.JobId,
            LeaseId = job.LeaseId,
            ResultType = manifest.Count > 0 ? "HasRecords" : "NoRecords",
            Records = manifest
                .Select(m => new JobResultRecord
                {
                    RecordType = m.RecordType,
                    RecordUrl = m.RecordUrl,
                    RecordId = m.RecordId,
                    SystemId = m.SystemId,
                })
                .ToList(),
        };

        await _findApiClient.SubmitAsync(token, result);

        if (_logger.IsEnabled(LogLevel.Information))
        {
            _logger.LogInformation("Submitted {Count} records", result.Records.Count);
        }
    }
}
