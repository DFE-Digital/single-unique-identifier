using SUI.GetAnIdentifier.Application.Models;

namespace SUI.GetAnIdentifier.Application.UnitTests.Services;

public class PdsSearchV1Tests
{
    [Fact]
    public void ShouldBuildQueries_And_OutputIsSpecificOrder()
    {
        // Arrange
        var sut = new PdsSearchV1();
        var person = new PersonSpecification
        {
            Given = "Jane",
            Family = "Doe",
            BirthDate = new DateOnly(1985, 4, 12),
            AddressPostalCode = "AB12 3CD",
            Gender = "female",
        };

        // Act
        var result = sut.BuildQuery(person);

        // Assert
        var keys = result.Keys.ToArray();
        var expectedOrder = new[]
        {
            "NonFuzzyGFD",
            "FuzzyGFD",
            "FuzzyAll",
            "NonFuzzyGFDRange",
            "NonFuzzyGFDRangePostcode",
            "FuzzyGFDRange",
            "FuzzyGFDRangePostcode",
        };

        Assert.Equal(expectedOrder.Length, keys.Length);
        Assert.Equal(expectedOrder, keys);
    }
}
