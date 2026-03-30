using System.Text.Json;
using Shouldly;
using SUI.Find.Application.Enums;
using SUI.Find.FindApi.Models;

namespace SUI.Find.FindApi.UnitTests.Models;

public class SearchResultsBaseTests
{
    [Fact]
    public void SearchResultsBase_Does_JsonDeserialize_AsExpected()
    {
        const string json = """
            {
                "Suid": "9691292211",
                "Status": "Running"
            }
            """;

        // ACT
        var sut = JsonSerializer.Deserialize<SearchResultsBase>(json);

        // ASSERT
        sut.ShouldBeEquivalentTo(
            new SearchResultsBase
            {
                Suid = "9691292211",
                Status = SearchStatus.Running,
                Items = [],
            }
        );
    }
}
