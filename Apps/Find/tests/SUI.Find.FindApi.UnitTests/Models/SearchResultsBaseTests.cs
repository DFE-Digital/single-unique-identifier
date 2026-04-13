using System.Text.Json;
using Shouldly;
using SUI.Find.Application.Enums;
using SUI.Find.FindApi.Models;

namespace SUI.Find.FindApi.UnitTests.Models;

public class SearchResultsBaseTests
{
    [Fact]
    public void Ctor_Test()
    {
        var sut = new SearchResultsBase { Suid = "9991234566", Status = SearchStatus.Completed };

        // ASSERT
        sut.Suid.ShouldBe("9991234566");
        sut.Status.ShouldBe(SearchStatus.Completed);
    }

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
