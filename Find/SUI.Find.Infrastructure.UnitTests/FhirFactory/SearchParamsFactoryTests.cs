using SUi.Find.Application.Models;
using SUi.Find.Infrastructure.Fhir;

namespace Sui.Find.Infrastructure.UnitTests.FhirFactory;

public class SearchParamsFactoryTests
{
    [Fact]
    public void Create_ShouldOutputExpectedSearchParams_FromSearchQuery()
    {
        // Arrange
        var query = new SearchQuery { Family = "Doe", Given = ["Jon"], Birthdate = ["eq1900-01-01"] };

        // Act
        var searchParams = SearchParamsFactory.Create(query);

        // Assert
        // Check regular parameters
        Assert.Contains(searchParams.Parameters, p => p is { Item1: "family", Item2: "Doe" });
        Assert.Contains(searchParams.Parameters, p => p is { Item1: "given", Item2: "Jon" });
        Assert.Contains(searchParams.Parameters, p => p is { Item1: "birthdate", Item2: "eq1900-01-01" });

        // Check that no unexpected parameters exist
        Assert.Equal(3, searchParams.Parameters.Count);

        // Check special properties if set (e.g., Count, Sort, etc.)
        Assert.Null(searchParams.Count);
        Assert.Empty(searchParams.Sort);
    }
}