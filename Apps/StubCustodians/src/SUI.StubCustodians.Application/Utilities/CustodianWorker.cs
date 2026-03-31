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
    private readonly IFindApiClient _client;
    private readonly IBaseUrlProvider _baseUrlProvider;
    private readonly Organisation _org;
    private readonly IServiceProvider _services;

    private readonly string _clientId;
    private readonly string _clientSecret;
    private readonly int _intervalSeconds;

    public CustodianWorker(
        ILogger<CustodianWorker> logger,
        ITokenProvider tokenProvider,
        IFindApiClient client,
        IBaseUrlProvider baseUrlProvider,
        Organisation org,
        IServiceProvider services
    )
    {
        _logger = logger;
        _tokenProvider = tokenProvider;
        _client = client;
        _baseUrlProvider = baseUrlProvider;
        _org = org;
        _services = services;

        var auth = org.Records.First().Connection.Auth;
        _clientId = auth.ClientId;
        _clientSecret = auth.ClientSecret;

        _intervalSeconds = Random.Shared.Next(30, 121);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (_logger.IsEnabled(LogLevel.Information))
            _logger.LogInformation(
                "Custodian {OrgId} started. Interval: {Interval}s",
                _org.OrgId,
                _intervalSeconds
            );

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var token = await _tokenProvider.GetTokenAsync(_clientId, _clientSecret);

                using var scope = _services.CreateScope();
                var manifestService = scope.ServiceProvider.GetRequiredService<IManifestService>();

                var job = await _client.ClaimAsync(token);

                if (job != null)
                {
                    await ProcessJob(job, token, manifestService, stoppingToken);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Polling failed for {OrgId}", _org.OrgId);
            }

            await Task.Delay(TimeSpan.FromSeconds(_intervalSeconds), stoppingToken);
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
                ["OrgId"] = _org.OrgId,
            }
        );

        if (_logger.IsEnabled(LogLevel.Information))
            _logger.LogInformation(
                "Processing job {JobDetails}",
                new
                {
                    job.JobId,
                    job.LeaseId,
                    _org.OrgId,
                }
            );

        if (string.IsNullOrWhiteSpace(job.Sui))
        {
            _logger.LogWarning("Job missing Sui");
            return;
        }

        var baseUrl = _baseUrlProvider.GetBaseUrl();

        var manifest = await manifestService.GetManifestForOrganisation(
            _org.OrgId,
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

        await _client.SubmitAsync(token, result);

        if (_logger.IsEnabled(LogLevel.Information))
            _logger.LogInformation("Submitted {Count} records", result.Records.Count);
    }
}
