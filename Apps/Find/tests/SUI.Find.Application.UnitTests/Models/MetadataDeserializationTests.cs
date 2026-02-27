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
                "RecordType": "Record1"
            }
            """;

        // Act
        var metadata = JsonSerializer.Deserialize<Metadata>(json);

        // Assert
        Assert.NotNull(metadata);
        Assert.Equal("Record1", metadata.RecordType);
        Assert.Equal(ApplicationConstants.SystemIds.Default, metadata.SystemId); // field-backed default
    }

    [Fact]
    public void SystemId_ShouldDefault_WhenJsonHasNullOrEmpty()
    {
        // null
        var jsonNull = """
            {
                "RecordType": "Record1",
                "SystemId": null
            }
            """;

        // Act
        var metadataNull = JsonSerializer.Deserialize<Metadata>(jsonNull);

        // Assert
        Assert.NotNull(metadataNull);
        Assert.Equal("Record1", metadataNull.RecordType);
        Assert.Equal(ApplicationConstants.SystemIds.Default, metadataNull.SystemId);

        // empty string
        var jsonEmpty = """
            {
                "RecordType": "Record1",
                "SystemId": ""
            }
            """;

        var metadataEmpty = JsonSerializer.Deserialize<Metadata>(jsonEmpty);

        // Assert
        Assert.NotNull(metadataEmpty);
        Assert.Equal("Record1", metadataEmpty.RecordType);
        Assert.Equal(ApplicationConstants.SystemIds.Default, metadataEmpty.SystemId);

        // whitespace
        var jsonWhitespace = """
            {
                "RecordType": "Record1",
                "SystemId": "   "
            }
            """;

        // Act
        var metadataWhitespace = JsonSerializer.Deserialize<Metadata>(jsonWhitespace);

        // Assert
        Assert.NotNull(metadataWhitespace);
        Assert.Equal("Record1", metadataWhitespace.RecordType);
        Assert.Equal(ApplicationConstants.SystemIds.Default, metadataWhitespace.SystemId);
    }

    [Fact]
    public void SystemId_ShouldPreserveValue_WhenJsonHasValue()
    {
        var json = """
            {
                "RecordType": "Record1",
                "SystemId": "CustomSystem"
            }
            """;

        var metadata = JsonSerializer.Deserialize<Metadata>(json);

        // Assert
        Assert.NotNull(metadata);
        Assert.Equal("Record1", metadata.RecordType);
        Assert.Equal("CustomSystem", metadata.SystemId);
    }
}
