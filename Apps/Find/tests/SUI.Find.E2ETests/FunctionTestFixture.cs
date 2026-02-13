using System.Net.Http.Json;
using Microsoft.Extensions.Configuration;
using Polly;
using Xunit.Abstractions;

namespace SUI.Find.E2ETests;

// ReSharper disable once ClassNeverInstantiated.Global - class is instantiated by XUnit
public class FunctionTestFixture : ICollectionFixture<FunctionTestFixture>, IDisposable
{
    public Config Config { get; }

    public HttpClient Client { get; }

    public HttpClient StubCustodiansClient { get; }

    private readonly SemaphoreSlim _mutex = new(1, 1);
    private Lazy<Task>? _upCheck;

    public FunctionTestFixture()
    {
        var configurationRoot = new ConfigurationBuilder()
            .AddEnvironmentVariables()
            .AddUserSecrets("SUI.E2E.Tests")
            .Build();

        Config = configurationRoot.GetSection("E2E").Get<Config>() ?? new Config();

        Client = new HttpClient { BaseAddress = new Uri(Config.BaseUrl) };

        StubCustodiansClient = new HttpClient
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

    private record HealthCheckResponse(string? Value);

    public async Task EnsureServicesAreUpAsync(ITestOutputHelper testOutputHelper)
    {
        await _mutex.WaitAsync();
        try
        {
            _upCheck ??= new Lazy<Task>(async () =>
                await Task.WhenAll(
                    EnsureServiceIsUpAsync("Find API", Client, testOutputHelper),
                    EnsureServiceIsUpAsync(
                        "StubCustodians API",
                        StubCustodiansClient,
                        testOutputHelper
                    )
                )
            );

            await _upCheck.Value;
        }
        finally
        {
            _mutex.Release();
        }
    }

    private static async Task EnsureServiceIsUpAsync(
        string serviceName,
        HttpClient client,
        ITestOutputHelper testOutputHelper
    )
    {
        const string url = "health";
        var waitInterval = TimeSpan.FromSeconds(10);

        testOutputHelper.WriteLine($"Checking {serviceName} is up: {client.BaseAddress}{url}");

        // If health check does not indicate healthy, wait and then retry
        const int retryCount = 3;
        var retryPolicy = Policy
            .HandleResult<bool>(healthy => !healthy)
            .WaitAndRetryAsync(
                retryCount,
                retryAttempt =>
                {
                    testOutputHelper.WriteLine(
                        $"{serviceName} does not indicate healthy, waiting for {waitInterval.Seconds} seconds, then retrying, retry {retryAttempt} / {retryCount}..."
                    );
                    return TimeSpan.FromSeconds(10);
                }
            );

        var healthy = await retryPolicy.ExecuteAsync(async () =>
        {
            try
            {
                using var response = await client.GetAsync(url);
                var content = response.Content.ReadFromJsonAsync<HealthCheckResponse>().Result;
                return content?.Value == "Healthy";
            }
            catch (Exception ex)
            {
                testOutputHelper.WriteLine(
                    $"Warning: health check exception ({serviceName}): {ex.Message}"
                );
                return false;
            }
        });

        Assert.True(healthy, $"The {serviceName} does not appear to be up and healthy");

        testOutputHelper.WriteLine($"{serviceName} is up 👍");
    }
}

[CollectionDefinition("E2E")]
public class FunctionTestCollectionFixture : ICollectionFixture<FunctionTestFixture> { }
