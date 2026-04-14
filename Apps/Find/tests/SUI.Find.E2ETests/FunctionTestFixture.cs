using System.Net.Http.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Http;
using Polly;
using Xunit.Abstractions;

namespace SUI.Find.E2ETests;

// ReSharper disable once ClassNeverInstantiated.Global - class is instantiated by XUnit
public class FunctionTestFixture : ICollectionFixture<FunctionTestFixture>, IDisposable
{
    public Config Config { get; }

    public HttpClient Client { get; }

    public HttpClient StubCustodiansClient { get; }

    public FunctionTestFixture()
    {
        var configurationRoot = new ConfigurationBuilder()
            .AddEnvironmentVariables()
            .AddUserSecrets("SUI.E2E.Tests")
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

    public void Dispose()
    {
        // MAYBE: Delete everything in storage as a cleanup operation?
        Client.Dispose();
        StubCustodiansClient.Dispose();
        GC.SuppressFinalize(this);
    }

    private record HealthCheckResponse(string? Value, string? BuildTimestamp);

    public async Task EnsureFindApiIsUpAsync(ITestOutputHelper testOutputHelper)
    {
        await EnsureServiceIsUpAsync(
            "Find API",
            Client,
            testOutputHelper,
            timeout: Config.UseExtendedHealthCheckTimeout
                ? TimeSpan.FromMinutes(10)
                : TimeSpan.FromSeconds(60),
            checkBuildTimestamp: FindApi.Utility.BuildTimestampUtility.BuildTimestamp
        );
    }

    public async Task EnsureStubCustodiansApiIsUpAsync(ITestOutputHelper testOutputHelper)
    {
        await EnsureServiceIsUpAsync(
            "StubCustodians API",
            StubCustodiansClient,
            testOutputHelper,
            timeout: TimeSpan.FromSeconds(60)
        );
    }

    public async Task EnsureServicesAreUpAsync(ITestOutputHelper testOutputHelper)
    {
        await Task.WhenAll(
            EnsureFindApiIsUpAsync(testOutputHelper),
            EnsureStubCustodiansApiIsUpAsync(testOutputHelper)
        );
    }

    private static async Task EnsureServiceIsUpAsync(
        string serviceName,
        HttpClient client,
        ITestOutputHelper testOutputHelper,
        TimeSpan timeout,
        string? checkBuildTimestamp = null
    )
    {
        const string url = "health";
        var waitInterval = TimeSpan.FromSeconds(10);

        testOutputHelper.WriteLine($"Checking {serviceName} is up: {client.BaseAddress}{url}");

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
                    && (checkBuildTimestamp == null || BuildTimestampMatches());

                bool BuildTimestampMatches()
                {
                    var buildTimestampMatches = content.BuildTimestamp == checkBuildTimestamp;
                    testOutputHelper.WriteLine(
                        buildTimestampMatches
                            ? $"{serviceName} build timestamp matches ({checkBuildTimestamp})"
                            : $"{serviceName} build timestamp does not yet match ({content.BuildTimestamp} != {checkBuildTimestamp})"
                    );
                    return buildTimestampMatches;
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

[CollectionDefinition("E2E")]
public class FunctionTestCollectionFixture : ICollectionFixture<FunctionTestFixture> { }
