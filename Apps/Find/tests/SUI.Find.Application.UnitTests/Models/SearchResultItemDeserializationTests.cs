using System.Text.Json;
using SUI.Find.Application.Constants;
using SUI.Find.Application.Models;

namespace SUI.Find.Application.UnitTests.Models;

public class SearchResultItemDeserializationTests
{
    [Fact]
    public void SystemId_ShouldDefault_WhenJsonMissingSystemId()
    {
        var json = """
            {
                "RecordType": "Type1",
                "RecordUrl": "url1"
            }
            """;

        var item = JsonSerializer.Deserialize<SearchResultItem>(json);

        Assert.NotNull(item);
        Assert.Equal(ApplicationConstants.SystemIds.Default, item.SystemId);
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
                "RecordUrl": "url1",
                "SystemId": "{{systemId}}"
            }
            """;

        // Act
        var item = JsonSerializer.Deserialize<SearchResultItem>(json);

        // Assert
        Assert.NotNull(item);
        Assert.Equal("Type1", item.RecordType);
        Assert.Equal(ApplicationConstants.SystemIds.Default, item.SystemId);
    }

    [Fact]
    public void SystemId_ShouldPreserve_WhenJsonHasValue()
    {
        var json = """
            {
                "RecordType": "Type1",
                "RecordUrl": "url1",
                "SystemId": "CustomSystem"
            }
            """;

        var item = JsonSerializer.Deserialize<SearchResultItem>(json);

        Assert.NotNull(item);
        Assert.Equal("CustomSystem", item.SystemId);
    }
}
