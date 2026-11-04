using System.Text.Json.Nodes;
using SUI.NotificationService.Application.Services;
using SUI.NotificationService.Application.UnitTests.Fixtures;

namespace SUI.NotificationService.Application.UnitTests.Services;

/// <summary>
/// Pins the parser to the example NHS England publishes for pds-record-change-2, unmodified. The
/// service tests build their payloads from the same file through MeshNotificationFixtures. The
/// sandbox template is the one separate copy, so it is held to the example here.
/// See https://digital.nhs.uk/developer/api-catalogue/multicast-notification-service/pds-change-event
/// </summary>
public sealed class MeshNotificationConformanceTests
{
    [Fact]
    public void TryParse_ShouldAcceptPublishedExample()
    {
        var parsed = MeshNotificationParser.TryParse(
            MeshNotificationFixtures.PublishedExample,
            out var bundle
        );

        Assert.True(parsed);
        Assert.NotNull(bundle);
    }

    [Fact]
    public void TryGetNhsNumber_ShouldReturnSubject_WhenGivenPublishedExample()
    {
        Assert.True(
            MeshNotificationParser.TryParse(
                MeshNotificationFixtures.PublishedExample,
                out var bundle
            )
        );

        var found = MeshNotificationParser.TryGetNhsNumber(bundle, out var nhsNumber);

        Assert.True(found);
        Assert.Equal(MeshNotificationFixtures.NhsNumber, nhsNumber);
    }

    [Fact]
    public void SandboxTemplate_ShouldMatchPublishedExample_WhenFilledWithItsValues()
    {
        var payload = File.ReadAllText(
                Path.Combine(
                    AppContext.BaseDirectory,
                    "Fixtures",
                    "pds-record-change-2-notification.template.json"
                )
            )
            .Replace("{{BUNDLE_ID}}", MeshNotificationFixtures.BundleId)
            .Replace("{{TIMESTAMP}}", MeshNotificationFixtures.Timestamp)
            .Replace("{{PARAMETERS_ID}}", MeshNotificationFixtures.ParametersId)
            .Replace("{{EVENT_NUMBER}}", "1")
            .Replace("{{NHS_NUMBER}}", MeshNotificationFixtures.NhsNumber)
            .Replace("{{VERSION_ID}}", "4");

        var expected = JsonNode.Parse(MeshNotificationFixtures.PublishedExample);
        var actual = JsonNode.Parse(payload);

        Assert.True(
            JsonNode.DeepEquals(expected, actual),
            $"Expected the published example but got:{Environment.NewLine}{actual}"
        );
    }
}
