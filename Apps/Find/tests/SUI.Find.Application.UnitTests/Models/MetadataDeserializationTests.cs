using System.Text.Json;
using SUI.Find.Application.Constants;
using SUI.Find.Application.Models.Matching;

namespace SUI.Find.Application.UnitTests.Models;

public class MetadataDeserializationTests
{
    [Fact]
    public void SystemId_ShouldDefault_WhenJsonMissingSystemId()
    {
        // Arrange
        var json = """
            {
                "RecordType": "Type1"
            }
            """;

        // Act
        var metadata = JsonSerializer.Deserialize<Metadata>(json);

        // Assert
        Assert.NotNull(metadata);
        Assert.Equal("Type1", metadata.RecordType);
        Assert.Equal(ApplicationConstants.SystemIds.Default, metadata.SystemId); // field-backed default
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void SystemId_ShouldDefault_WhenJsonHasNullOrEmpty(string? systemId)
    {
        // Arrange
        var json = $$"""
            {
                "RecordType": "Type1",
                "SystemId": "{{systemId}}"
            }
            """;

        // Act
        var metadata = JsonSerializer.Deserialize<Metadata>(json);

        // Assert
        Assert.NotNull(metadata);
        Assert.Equal("Type1", metadata.RecordType);
        Assert.Equal(ApplicationConstants.SystemIds.Default, metadata.SystemId);
    }

    [Fact]
    public void SystemId_ShouldPreserveValue_WhenJsonHasValue()
    {
        var json = """
            {
                "RecordType": "Type1",
                "SystemId": "CustomSystem"
            }
            """;

        var metadata = JsonSerializer.Deserialize<Metadata>(json);

        // Assert
        Assert.NotNull(metadata);
        Assert.Equal("Type1", metadata.RecordType);
        Assert.Equal("CustomSystem", metadata.SystemId);
    }
}
