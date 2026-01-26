using SUI.Find.Application.Models.Matching;
using SUI.Find.Application.Services.PdsSearch;

namespace SUI.Find.ApplicationTests.Services.PdsSearch;

public class SearchQueryBuilderTests
{
    [Theory]
    [InlineData("AB12 3CD", "AB*")]
    [InlineData("AB123CD", "AB*")]
    public void AddsPostcodeWildcardCorrectly(string postcode, string expected)
    {
        var builder = new SearchQueryBuilder(
            new SearchSpecification
            {
                BirthDate = DateOnly.FromDateTime(DateTime.Now),
                AddressPostalCode = postcode,
            }
        );

        builder.AddFuzzyGfdRangePostcodeWildcard();

        var result = builder.Build();

        var query = result.ContainsKey("FuzzyGFDRangePostcodeWildcard");
        Assert.True(query);
        Assert.Equal(expected, result.Values.First().AddressPostalcode);
    }

    [Fact]
    public void ShouldIncludeHistoryOnNonFuzzyQueries()
    {
        var builder = new SearchQueryBuilder(
            new SearchSpecification
            {
                Given = "John",
                Family = "Doe",
                BirthDate = DateOnly.FromDateTime(new DateTime(2010, 1, 1)),
            }
        );

        builder.AddNonFuzzyGfd();
        builder.AddNonFuzzyGfdRange();
        builder.AddNonFuzzyAll();

        var result = builder.Build();

        foreach (var query in result.Values)
        {
            Assert.True(query.History);
        }
    }

    [Theory]
    [InlineData("John", new[] { "John" }, "Smith", "Smith")]
    [InlineData("John James Steve", new[] { "John", "James", "Steve" }, "Smith (Jones)", "Smith")]
    [InlineData("John-James Steve", new[] { "John-James", "Steve" }, "Smith-Jones", "Smith-Jones")]
    public void AddNonFuzzyGfdAddsPreprocessedNamesCorrectly(
        string given,
        string[] givenExpected,
        string family,
        string familyExpected
    )
    {
        var builder = new SearchQueryBuilder(
            new SearchSpecification
            {
                Given = given,
                Family = family,
                BirthDate = DateOnly.FromDateTime(new DateTime(2010, 1, 1)),
            },
            preprocessNames: true
        );

        builder.AddNonFuzzyGfd();

        var result = builder.Build();

        var query = result.ContainsKey("NonFuzzyGFD");
        Assert.True(query);
        Assert.Equal(givenExpected, result.Values.First().Given);
        Assert.Equal(familyExpected, result.Values.First().Family);
    }
}
