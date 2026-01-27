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
            new PersonSpecification
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
            new PersonSpecification
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
            new PersonSpecification
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

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void AddNonFuzzyGfdRangePostcode_ShouldAddPostcodeWithoutWhitespace(
        bool usePostcodeWildcard
    )
    {
        var builder = new SearchQueryBuilder(
            new PersonSpecification
            {
                BirthDate = DateOnly.FromDateTime(DateTime.Now),
                AddressPostalCode = "AB1 2CD",
            }
        );

        builder.AddNonFuzzyGfdRangePostcode(usePostcodeWildcard);

        var result = builder.Build();

        if (usePostcodeWildcard)
        {
            Assert.True(result.ContainsKey("NonFuzzyGFDRangePostcodeWildcard"));
            Assert.Equal("AB*", result.Values.First().AddressPostalcode);
        }
        else
        {
            Assert.True(result.ContainsKey("NonFuzzyGFDRangePostcode"));
            Assert.Equal("AB1 2CD", result.Values.First().AddressPostalcode);
        }
    }

    [Fact]
    public void AddNonFuzzyAllPostcodeWildcard_ShouldAddPostcodeWithWildcard()
    {
        var builder = new SearchQueryBuilder(
            new PersonSpecification
            {
                BirthDate = DateOnly.FromDateTime(DateTime.Now),
                AddressPostalCode = "AB1 2CD",
            }
        );

        builder.AddNonFuzzyAllPostcodeWildcard();

        var result = builder.Build();

        Assert.True(result.ContainsKey("NonFuzzyAllPostcodeWildcard"));
        Assert.Equal("AB*", result.Values.First().AddressPostalcode);
    }

    [Fact]
    public void NonFuzzyGFDPostcode_ShouldAddPostcode()
    {
        var builder = new SearchQueryBuilder(
            new PersonSpecification
            {
                BirthDate = DateOnly.FromDateTime(DateTime.Now),
                AddressPostalCode = "AB1 2CD",
            }
        );

        builder.AddNonFuzzyGfdPostcode();

        var result = builder.Build();

        Assert.True(result.ContainsKey("NonFuzzyGFDPostcode"));
        Assert.Equal("AB1 2CD", result.Values.First().AddressPostalcode);
    }

    [Fact]
    public void AddFuzzyGfdPostcodeWildcard_ShouldAddPostcodeWithWildcard()
    {
        var builder = new SearchQueryBuilder(
            new PersonSpecification
            {
                BirthDate = DateOnly.FromDateTime(DateTime.Now),
                AddressPostalCode = "AB1 2CD",
            }
        );

        builder.AddFuzzyGfdPostcodeWildcard();

        var result = builder.Build();

        Assert.True(result.ContainsKey("FuzzyGFDPostcodeWildcard"));
        Assert.Equal("AB*", result.Values.First().AddressPostalcode);
    }

    [Fact]
    public void AddExactGfd_ShouldReturnTrueToExact()
    {
        var builder = new SearchQueryBuilder(
            new PersonSpecification
            {
                Given = "John",
                Family = "Doe",
                BirthDate = DateOnly.FromDateTime(new DateTime(2010, 1, 1)),
            }
        );

        builder.AddExactGfd();

        var result = builder.Build();

        var query = result.ContainsKey("ExactGFD");
        Assert.True(query);
        Assert.True(result.Values.First().ExactMatch);
    }

    [Fact]
    public void AddExactAll_ShouldReturnTrueToExact()
    {
        var builder = new SearchQueryBuilder(
            new PersonSpecification
            {
                Given = "John",
                Family = "Doe",
                BirthDate = DateOnly.FromDateTime(new DateTime(2010, 1, 1)),
            }
        );

        builder.AddExactAll();

        var result = builder.Build();

        var query = result.ContainsKey("ExactAll");
        Assert.True(query);
        Assert.True(result.Values.First().ExactMatch);
    }
}
