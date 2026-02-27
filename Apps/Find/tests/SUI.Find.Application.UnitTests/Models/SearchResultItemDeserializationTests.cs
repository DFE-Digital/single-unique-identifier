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

    [Fact]
    public void SystemId_ShouldDefault_WhenJsonHasNullOrEmpty()
    {
        // null
        var jsonNull = """
            {
                "RecordType": "Type1",
                "RecordUrl": "url1",
                "SystemId": null
            }
            """;

        // Act
        var itemNull = JsonSerializer.Deserialize<SearchResultItem>(
            jsonNull,
            JsonSerializerOptions.Web
        );

        // Assert
        Assert.NotNull(itemNull);
        Assert.Equal("Type1", itemNull.RecordType);
        Assert.Equal(ApplicationConstants.SystemIds.Default, itemNull.SystemId);

        // empty string
        var jsonEmpty = """
            {
                "RecordType": "Type1",
                "RecordUrl": "url1",
                "SystemId": ""
            }
            """;

        var itemEmpty = JsonSerializer.Deserialize<SearchResultItem>(
            jsonEmpty,
            JsonSerializerOptions.Web
        );

        // Assert
        Assert.NotNull(itemEmpty);
        Assert.Equal("Type1", itemEmpty.RecordType);
        Assert.Equal(ApplicationConstants.SystemIds.Default, itemEmpty.SystemId);

        // whitespace
        var jsonWhitespace = """
            {
                "RecordType": "Type1",
                "RecordUrl": "url1",
                "SystemId": "   "
            }
            """;

        // Act
        var itemWhitespace = JsonSerializer.Deserialize<SearchResultItem>(jsonWhitespace);

        // Assert
        Assert.NotNull(itemWhitespace);
        Assert.Equal("Type1", itemWhitespace.RecordType);
        Assert.Equal(ApplicationConstants.SystemIds.Default, itemWhitespace.SystemId);
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
