using Hl7.Fhir.Model;
using SUI.NotificationService.Application.Services;
using SUI.NotificationService.UnitTests.Fixtures;

namespace SUI.NotificationService.Application.UnitTests.Services;

public sealed class MeshNotificationParserTests
{
    [Fact]
    public void TryParse_ReturnsBundle_WhenBodyIsRecordChangeNotification()
    {
        var parsed = MeshNotificationParser.TryParse(BuildNotification(), out var bundle);

        Assert.True(parsed);
        Assert.NotNull(bundle);
        Assert.Equal(MeshNotificationFixtures.BundleId, bundle.Id);
        Assert.Equal(Bundle.BundleType.History, bundle.Type);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("not json at all")]
    [InlineData("{\"resourceType\":\"Bundle\"")]
    public void TryParse_ReturnsFalse_WhenBodyIsNotParseable(string content)
    {
        var parsed = MeshNotificationParser.TryParse(content, out var bundle);

        Assert.False(parsed);
        Assert.Null(bundle);
    }

    [Fact]
    public void TryParse_ReturnsFalse_WhenBodyIsNotAFhirResource()
    {
        var parsed = MeshNotificationParser.TryParse("{\"some\":\"object\"}", out var bundle);

        Assert.False(parsed);
        Assert.Null(bundle);
    }

    [Fact]
    public void TryParse_ReturnsFalse_WhenBundleIsNotAHistory()
    {
        var content = BuildNotification()
            .Replace("\"type\": \"history\"", "\"type\": \"searchset\"");

        var parsed = MeshNotificationParser.TryParse(content, out var bundle);

        Assert.False(parsed);
        Assert.Null(bundle);
    }

    [Fact]
    public void TryParse_ReturnsBundle_RegardlessOfDeclaredProfile()
    {
        var content = BuildNotification().Replace("http://hl7.org/", "https://hl7.org/");

        var parsed = MeshNotificationParser.TryParse(content, out var bundle);

        Assert.True(parsed);
        Assert.NotNull(bundle);
    }

    [Fact]
    public void TryParse_ReturnsFalse_WhenFirstEntryIsNotParameters()
    {
        var content = """
            {
              "resourceType": "Bundle",
              "id": "d8f1a2b4-0c3d-4e5f-9a6b-7c8d9e0f1a2b",
              "type": "history",
              "timestamp": "2026-09-22T09:15:00+00:00",
              "entry": [
                {
                  "fullUrl": "urn:uuid:3f2c1d9e-5b6a-4c7d-8e9f-0a1b2c3d4e5f",
                  "resource": { "resourceType": "Basic", "id": "3f2c1d9e-5b6a-4c7d-8e9f-0a1b2c3d4e5f" },
                  "request": { "method": "GET", "url": "Basic/3f2c1d9e-5b6a-4c7d-8e9f-0a1b2c3d4e5f" },
                  "response": { "status": "200" }
                }
              ]
            }
            """;

        var parsed = MeshNotificationParser.TryParse(content, out var bundle);

        Assert.False(parsed);
        Assert.Null(bundle);
    }

    [Fact]
    public void TryParse_ReturnsFalse_WhenBundleHasNoEntries()
    {
        var content = """
            {
              "resourceType": "Bundle",
              "id": "d8f1a2b4-0c3d-4e5f-9a6b-7c8d9e0f1a2b",
              "type": "history",
              "timestamp": "2026-09-22T09:15:00+00:00"
            }
            """;

        var parsed = MeshNotificationParser.TryParse(content, out var bundle);

        Assert.False(parsed);
        Assert.Null(bundle);
    }

    [Fact]
    public void TryGetNhsNumber_ReturnsNhsNumber_WhenNotificationCarriesASubject()
    {
        var bundle = Parse(BuildNotification());

        var found = MeshNotificationParser.TryGetNhsNumber(bundle, out var nhsNumber);

        Assert.True(found);
        Assert.Equal(MeshNotificationFixtures.NhsNumber, nhsNumber);
    }

    [Fact]
    public void TryGetNhsNumber_ReturnsFalse_WhenSubjectPartIsMissing()
    {
        var bundle = Parse(BuildNotification(nhsNumber: null));

        var found = MeshNotificationParser.TryGetNhsNumber(bundle, out var nhsNumber);

        Assert.False(found);
        Assert.Null(nhsNumber);
    }

    [Fact]
    public void TryGetNhsNumber_ReturnsFalse_WhenSubjectIdentifierIsBlank()
    {
        // An empty identifier is not valid FHIR and never gets this far, so blank means whitespace.
        var bundle = Parse(BuildNotification("   "));

        var found = MeshNotificationParser.TryGetNhsNumber(bundle, out var nhsNumber);

        Assert.False(found);
        Assert.Null(nhsNumber);
    }

    [Fact]
    public void TryGetNhsNumber_ReturnsFalse_WhenBundleHasNoParametersEntry()
    {
        var found = MeshNotificationParser.TryGetNhsNumber(new Bundle(), out var nhsNumber);

        Assert.False(found);
        Assert.Null(nhsNumber);
    }

    private static Bundle Parse(string content)
    {
        Assert.True(MeshNotificationParser.TryParse(content, out var bundle));
        return bundle!;
    }

    private static string BuildNotification(
        string? nhsNumber = MeshNotificationFixtures.NhsNumber
    ) => MeshNotificationFixtures.BuildNotification(nhsNumber);
}
