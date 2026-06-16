using System.Text.Json;
using Microsoft.Extensions.Configuration;
using NSubstitute;
using NSubstitute.Extensions;
using SUI.StubCustodians.Application.Services;

namespace SUI.StubCustodians.Application.Unit.Tests.Services;

public class FindApiAuthClientProviderTests
{
    private readonly IConfiguration _mockConfiguration = Substitute.For<IConfiguration>();

    private readonly FindApiAuthClientProvider _sut;

    public FindApiAuthClientProviderTests()
    {
        _mockConfiguration.ReturnsForAll((string?)null);

        _sut = new FindApiAuthClientProvider(_mockConfiguration);
    }

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

            // ACT
            var clients = _sut.GetAuthClients();

            // ASSERT
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

            // ACT
            var first = _sut.GetAuthClients();
            var second = _sut.GetAuthClients();

            // ASSERT
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

        // ACT & ASSERT
        var ex = Assert.Throws<InvalidOperationException>(() => _sut.GetAuthClients());

        Assert.Contains("auth-clients-inbound.json not found", ex.Message);
    }

    [Fact]
    public void GetAuthClients_ShouldThrow_WhenJsonInvalid()
    {
        try
        {
            WriteJson("invalid-json");

            // ACT & ASSERT
            Assert.ThrowsAny<JsonException>(() => _sut.GetAuthClients());
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

            // ACT
            var result = _sut.GetAuthClients();

            // ASSERT
            Assert.Empty(result);
        }
        finally
        {
            Cleanup();
        }
    }
}
