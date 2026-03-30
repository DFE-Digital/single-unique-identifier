using System.Text.Json;
using SUI.StubCustodians.Application.Services;

namespace SUI.StubCustodians.Application.Unit.Tests.Services;

public class OrgDirectoryProviderTests
{
    private static string DataDirectory => Path.Combine(AppContext.BaseDirectory, "Data");

    private static string FilePath => Path.Combine(DataDirectory, "org-directory.json");

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
    public void GetOrganisations_ShouldLoadOrganisations_FromJsonFile()
    {
        try
        {
            var json = """
                {
                  "organisations": [
                    {
                      "orgId": "ORG-1",
                      "records": []
                    }
                  ]
                }
                """;

            WriteJson(json);

            var provider = new OrgDirectoryProvider();

            var orgs = provider.GetOrganisations();

            Assert.Single(orgs);
            Assert.Equal("ORG-1", orgs.First().OrgId);
        }
        finally
        {
            Cleanup();
        }
    }

    [Fact]
    public void GetOrganisations_ShouldCacheResult_WhenCalledMultipleTimes()
    {
        try
        {
            var json = """
                {
                  "organisations": [
                    { "orgId": "ORG-1", "records": [] }
                  ]
                }
                """;

            WriteJson(json);

            var provider = new OrgDirectoryProvider();

            var first = provider.GetOrganisations();
            var second = provider.GetOrganisations();

            Assert.Same(first, second);
        }
        finally
        {
            Cleanup();
        }
    }

    [Fact]
    public void GetOrganisations_ShouldThrow_WhenFileMissing()
    {
        Cleanup();

        var provider = new OrgDirectoryProvider();

        var ex = Assert.Throws<InvalidOperationException>(() => provider.GetOrganisations());

        Assert.Contains("org-directory.json not found", ex.Message);
    }

    [Fact]
    public void GetOrganisations_ShouldThrow_WhenJsonInvalid()
    {
        try
        {
            WriteJson("invalid-json");

            var provider = new OrgDirectoryProvider();

            Assert.ThrowsAny<JsonException>(() => provider.GetOrganisations());
        }
        finally
        {
            Cleanup();
        }
    }

    [Fact]
    public void GetOrganisations_ShouldReturnEmptyList_WhenJsonHasNoOrganisations()
    {
        try
        {
            WriteJson("{}");

            var provider = new OrgDirectoryProvider();

            var result = provider.GetOrganisations();

            Assert.Empty(result);
        }
        finally
        {
            Cleanup();
        }
    }
}
