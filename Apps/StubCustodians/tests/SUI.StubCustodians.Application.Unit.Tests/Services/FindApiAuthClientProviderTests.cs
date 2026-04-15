using System.Text.Json;
using SUI.StubCustodians.Application.Services;

namespace SUI.StubCustodians.Application.Unit.Tests.Services;

public class FindApiAuthClientProviderTests
{
    private static string DataDirectory => Path.Combine(AppContext.BaseDirectory, "Data");

    private static string FilePath => Path.Combine(DataDirectory, "auth-clients-inbound.json");

    private static void WriteJson(string json)
    {
        Directory.CreateDirectory(DataDirectory);
        File.WriteAllText(FilePath, json);
    }

    private static void Cleanup()
    {
        if (File.Exists(FilePath))
            File.Delete(FilePath);
    }

    [Fact]
    public void GetAuthClients_ShouldLoadOrganisations_FromJsonFile()
    {
        try
        {
            var json = """
                {
                  "clients": [
                    {
                      "enabled": true,
                      "clientId": "LOCAL-AUTHORITY-01",
                      "clientSecret": "SUIProject",
                      "allowedScopes": [
                        "work-item.read",
                        "work-item.write"
                      ]
                    }
                  ]
                }
                """;

            WriteJson(json);

            var provider = new FindApiAuthClientProvider();

            var clients = provider.GetAuthClients();

            Assert.Single(clients);
            Assert.Equal("LOCAL-AUTHORITY-01", clients[0].ClientId);
        }
        finally
        {
            Cleanup();
        }
    }

    [Fact]
    public void GetAuthClients_ShouldCacheResult_WhenCalledMultipleTimes()
    {
        try
        {
            var json = """
                {
                  "clients": [
                    {
                      "enabled": true,
                      "clientId": "LOCAL-AUTHORITY-01",
                      "clientSecret": "SUIProject",
                      "allowedScopes": [
                        "work-item.read",
                        "work-item.write"
                      ]
                    }
                  ]
                }
                """;

            WriteJson(json);

            var provider = new FindApiAuthClientProvider();

            var first = provider.GetAuthClients();
            var second = provider.GetAuthClients();

            Assert.Same(first, second);
        }
        finally
        {
            Cleanup();
        }
    }

    [Fact]
    public void GetAuthClients_ShouldThrow_WhenFileMissing()
    {
        Cleanup();

        var provider = new FindApiAuthClientProvider();

        var ex = Assert.Throws<InvalidOperationException>(() => provider.GetAuthClients());

        Assert.Contains("auth-clients-inbound.json not found", ex.Message);
    }

    [Fact]
    public void GetAuthClients_ShouldThrow_WhenJsonInvalid()
    {
        try
        {
            WriteJson("invalid-json");

            var provider = new FindApiAuthClientProvider();

            Assert.ThrowsAny<JsonException>(() => provider.GetAuthClients());
        }
        finally
        {
            Cleanup();
        }
    }

    [Fact]
    public void GetAuthClients_ShouldReturnEmptyList_WhenJsonHasNoOrganisations()
    {
        try
        {
            WriteJson("{}");

            var provider = new FindApiAuthClientProvider();

            var result = provider.GetAuthClients();

            Assert.Empty(result);
        }
        finally
        {
            Cleanup();
        }
    }
}
