using Microsoft.Extensions.Configuration;

namespace SUI.Find.E2ETests;

public class FunctionTestFixture : IDisposable
{
    public Config Config { get; }

    public HttpClient Client { get; }

    public FunctionTestFixture()
    {
        var configurationRoot = new ConfigurationBuilder()
            .AddEnvironmentVariables()
            .AddUserSecrets("SUI.E2E.Tests")
            .Build();

        Config = configurationRoot.GetSection("E2E").Get<Config>() ?? new Config();

        Client = new HttpClient { BaseAddress = new Uri(Config.BaseUrl) };
    }

    public void Dispose()
    {
        // MAYBE: Delete everything in storage as a cleanup operation?
        Client.Dispose();
        GC.SuppressFinalize(this);
    }
}
