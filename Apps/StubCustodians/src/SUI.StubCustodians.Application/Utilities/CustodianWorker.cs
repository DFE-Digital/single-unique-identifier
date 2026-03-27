using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SUI.StubCustodians.Application.Interfaces;
using SUI.StubCustodians.Application.Models;

namespace SUI.StubCustodians.Application.Utilities;

public class CustodianWorker : BackgroundService
{
    private readonly ILogger<CustodianWorker> _logger;
    private readonly TokenProvider _tokenProvider;
    private readonly FindApiClient _client;
    private readonly IManifestService _manifestService;
    private readonly IConfiguration _config;
    private readonly IRandomDelayService _delayService;

    private readonly Organisation _org;
    private readonly string _clientId;
    private readonly string _clientSecret;
    private readonly int _intervalSeconds;

    public CustodianWorker(
        ILogger<CustodianWorker> logger,
        TokenProvider tokenProvider,
        FindApiClient client,
        IManifestService manifestService,
        IConfiguration config,
        Organisation org,
        IRandomDelayService delayService
    )
    {
        _logger = logger;
        _tokenProvider = tokenProvider;
        _client = client;
        _manifestService = manifestService;
        _config = config;
        _org = org;
        _delayService = delayService;

        var auth = org.Records.First().Connection.Auth;

        _clientId = auth.ClientId;
        _clientSecret = auth.ClientSecret;

        _intervalSeconds = Random.Shared.Next(30, 121);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
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

                JobInfo? job;
                do
                {
                    job = await _client.ClaimAsync(token);

                    if (job != null)
                    {
                        await ProcessJob(job, token, stoppingToken);
                    }
                } while (job != null);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Polling failed for {OrgId}", _org.OrgId);
            }

            await Task.Delay(TimeSpan.FromSeconds(_intervalSeconds), stoppingToken);
        }
    }

    private async Task ProcessJob(JobInfo job, string token, CancellationToken ct)
    {
        using var scope = _logger.BeginScope(
            new Dictionary<string, object>
            {
                ["JobId"] = job.JobId,
                ["LeaseId"] = job.LeaseId,
                ["OrgId"] = _org.OrgId,
            }
        );

        // AC: random delay
        await _delayService.DelayAsync(ct);

        if (string.IsNullOrWhiteSpace(job.Sui))
        {
            _logger.LogWarning("Job missing Sui");
            return;
        }

        var baseUrl = _config["StubCustodians:BaseUrl"]!;

        var manifest = await _manifestService.GetManifestForOrganisation(
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

        _logger.LogInformation("Submitted {Count} records", result.Records.Count);
    }
}
