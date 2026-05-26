using System.Net.Http.Json;
using Azure;
using Azure.Data.Tables;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Http;
using Polly;

namespace SUI.Find.E2ETests;

// ReSharper disable once ClassNeverInstantiated.Global - class is instantiated by XUnit
public class FunctionTestFixture : IAsyncLifetime
{
    private bool _tableResetComplete;
    private readonly SemaphoreSlim _resetTablesMutex = new(1, 1);

    public Config Config { get; }

    public HttpClient Client { get; }

    public HttpClient StubCustodiansClient { get; }

    public FunctionTestFixture()
    {
        var configurationRoot = new ConfigurationBuilder()
            .AddEnvironmentVariables()
            .AddUserSecrets<FunctionTestFixture>()
            .Build();

        Config = configurationRoot.GetSection("E2E").Get<Config>() ?? new Config();

        // For our HTTP clients, retry a small number of times if we ever receive a timeout, with a small wait in between
        var retryPolicy = Policy<HttpResponseMessage>
            .Handle<Exception>(IsTimeoutError)
            .WaitAndRetryAsync(retryCount: 3, sleepDurationProvider: _ => TimeSpan.FromSeconds(2));

        var policyHandler = new PolicyHttpMessageHandler(retryPolicy);
        policyHandler.InnerHandler = new HttpClientHandler();

        Client = new HttpClient(policyHandler) { BaseAddress = new Uri(Config.BaseUrl) };

        StubCustodiansClient = new HttpClient(policyHandler)
        {
            BaseAddress = new Uri(Config.StubCustodiansBaseUrl),
        };
    }

    public ValueTask InitializeAsync() => ValueTask.CompletedTask;

    public ValueTask DisposeAsync()
    {
        // MAYBE: Delete everything in storage as a cleanup operation?
        Client.Dispose();
        StubCustodiansClient.Dispose();
        GC.SuppressFinalize(this);
        return ValueTask.CompletedTask;
    }

    private record HealthCheckResponse(string? Value, DateTimeOffset? BuildTimestamp);

    public async Task EnsureFindApiIsUpAsync(ITestOutputHelper testOutputHelper)
    {
        await EnsureTablesResetOneTimeInitAsync(testOutputHelper);

        await EnsureServiceIsUpAsync(
            "Find API",
            Client,
            testOutputHelper,
            checkBuildTimestampThreshold: Config.CheckFindApiBuildTimestampThreshold
        );
    }

    public async Task EnsureStubCustodiansApiIsUpAsync(ITestOutputHelper testOutputHelper)
    {
        await EnsureServiceIsUpAsync(
            "StubCustodians API",
            StubCustodiansClient,
            testOutputHelper,
            checkBuildTimestampThreshold: Config.CheckStubCustodiansApiBuildTimestampThreshold
        );
    }

    public async Task EnsureServicesAreUpAsync(ITestOutputHelper testOutputHelper)
    {
        TestContext.Current.SendDiagnosticMessage(
            "Checking Find API and StubCustodians API health..."
        );

        await Task.WhenAll(
            EnsureFindApiIsUpAsync(testOutputHelper),
            EnsureStubCustodiansApiIsUpAsync(testOutputHelper)
        );
    }

    private async Task EnsureTablesResetOneTimeInitAsync(ITestOutputHelper testOutputHelper)
    {
        if (Config.SkipResetAzureTables)
        {
            return;
        }

        if (_tableResetComplete)
        {
            return;
        }

        await _resetTablesMutex.WaitAsync();
        try
        {
            if (_tableResetComplete)
            {
                return;
            }

            var service = new TableServiceClient(Config.FindApiStorageConnectionString);
            var tableNames = new[]
            {
                "ResultsUrlMappings",
                "TestHubNameHistory",
                "TestHubNameInstances",
                "Jobs",
                "WorkItemJobCounts",
            };

            testOutputHelper.WriteLine($"Resetting Azure Tables: {string.Join(", ", tableNames)}");

            foreach (var table in tableNames)
            {
                try
                {
                    await service.DeleteTableAsync(table);
                }
                catch (RequestFailedException ex) when (ex.Status == 404)
                {
                    // Table didn't exist; ignore
                }

                await service.CreateTableIfNotExistsAsync(table);
            }

            testOutputHelper.WriteLine(
                $"Reset Azure Tables complete: {string.Join(", ", tableNames)}"
            );

            _tableResetComplete = true;
        }
        finally
        {
            _resetTablesMutex.Release();
        }
    }

    private static async Task EnsureServiceIsUpAsync(
        string serviceName,
        HttpClient client,
        ITestOutputHelper testOutputHelper,
        DateTimeOffset? checkBuildTimestampThreshold = null
    )
    {
        const string url = "health";
        var waitInterval = TimeSpan.FromSeconds(10);

        testOutputHelper.WriteLine($"Checking {serviceName} is up: {client.BaseAddress}{url}");

        var useExtendedTimeout = checkBuildTimestampThreshold != null;
        var timeout = useExtendedTimeout ? TimeSpan.FromMinutes(10) : TimeSpan.FromSeconds(60);

        // If health check does not indicate healthy, wait and then retry
        var retryCount = (int)Math.Round(timeout / waitInterval);
        var retryPolicy = Policy
            .HandleResult<bool>(healthy => !healthy)
            .WaitAndRetryAsync(
                retryCount,
                retryAttempt =>
                {
                    testOutputHelper.WriteLine(
                        $"{serviceName} does not indicate healthy, waiting for {waitInterval.Seconds} seconds, then retrying, retry {retryAttempt} / {retryCount}..."
                    );
                    return waitInterval;
                }
            );

        var healthy = await retryPolicy.ExecuteAsync(async () =>
        {
            try
            {
                using var response = await client.GetAsync(url);
                var content = response.Content.ReadFromJsonAsync<HealthCheckResponse>().Result;

                return content?.Value == "Healthy"
                    && (checkBuildTimestampThreshold == null || CheckBuildTimestampThreshold());

                // When used, this check causes the tests to wait until the API responds with a build timestamp which is on or after a known point in time that confirms the latest version is in use.
                // This resolves false failures which can occur when the e2e tests are triggered immediately after deployment.
                bool CheckBuildTimestampThreshold()
                {
                    var isBuiltSinceThreshold =
                        content.BuildTimestamp >= checkBuildTimestampThreshold;
                    testOutputHelper.WriteLine(
                        isBuiltSinceThreshold
                            ? $"{serviceName} build timestamp satisfies threshold (build timestamp {content.BuildTimestamp:O} is on or after threshold {checkBuildTimestampThreshold:O})"
                            : $"{serviceName} build timestamp does not satisfy threshold (build timestamp {content.BuildTimestamp:O} is NOT on or after threshold {checkBuildTimestampThreshold:O})"
                    );
                    return isBuiltSinceThreshold;
                }
            }
            catch (Exception ex)
            {
                testOutputHelper.WriteLine(
                    $"Warning: health check exception ({serviceName}): {ex.GetType().Name}: {ex.Message}"
                );

                return false;
            }
        });

        Assert.True(healthy, $"The {serviceName} does not appear to be up and healthy");

        testOutputHelper.WriteLine($"{serviceName} is up 👍");
    }

    private static bool IsTimeoutError(Exception? ex)
    {
        if (ex == null)
        {
            return false;
        }

        if (ex is AggregateException agg)
        {
            return agg.InnerExceptions.Any(IsTimeoutError);
        }

        var exceptions = new List<Exception>([ex]);
        while (ex.InnerException != null)
        {
            exceptions.Add(ex.InnerException);
            ex = ex.InnerException;
        }

        // Note it is fine to use TaskCanceledException in these e2e tests, because cancellation tokens aren't being used to actually cancel tests.
        // However, this approach should not be copied in to production code.
        var isTimeout =
            exceptions.OfType<TimeoutException>().Any()
            || exceptions.OfType<TaskCanceledException>().Any();
        return isTimeout;
    }
}
