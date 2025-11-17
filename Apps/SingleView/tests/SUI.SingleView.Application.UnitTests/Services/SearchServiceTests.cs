using Bogus;
using Shouldly;
using SUI.SingleView.Application.Services;
using SUI.SingleView.Domain.Models;
using SUI.SingleView.Domain.UnitTests.Extensions;

namespace SUI.SingleView.Application.UnitTests.Services;

public class SearchServiceTests
{
    private readonly SearchService _searchService = new();
    private readonly Faker _faker = new("en_GB");

    [Fact]
    public void Search_WithNhsNumber_ReturnsResults()
    {
        // Arrange
        var nhsNumber = _faker.GenerateNhsNumber().Value;

        // Act
        var result = _searchService.Search(nhsNumber);

        // Assert
        result.ShouldNotBeNull();
        result.ShouldBeOfType<List<SearchResult>>();
    }

    [Fact]
    public void Search_WithEmptyNhsNumber_ReturnsResults()
    {
        // Arrange
        var nhsNumber = string.Empty;

        // Act
        var result = _searchService.Search(nhsNumber);

        // Assert
        result.ShouldNotBeNull();
        result.ShouldBeOfType<List<SearchResult>>();
    }

    [Fact]
    public void Search_WithNullNhsNumber_ReturnsResults()
    {
        // Arrange
        string nhsNumber = null!;

        // Act
        var result = _searchService.Search(nhsNumber);

        // Assert
        result.ShouldNotBeNull();
        result.ShouldBeOfType<List<SearchResult>>();
    }

    [Fact]
    public void Search_WithPersonalDetails_ReturnsResults()
    {
        // Arrange
        var firstName = _faker.Person.FirstName;
        var lastName = _faker.Person.LastName;
        var dateOfBirth = _faker.Date.Past(18);
        var houseNumberOrName = _faker.Address.BuildingNumber();
        var postcode = _faker.Address.ZipCode();

        // Act
        var result = _searchService.Search(
            firstName,
            lastName,
            dateOfBirth,
            houseNumberOrName,
            postcode
        );

        // Assert
        result.ShouldNotBeNull();
        result.ShouldBeOfType<List<SearchResult>>();
    }

    [Fact]
    public void Search_WithOnlyFirstName_ReturnsResults()
    {
        // Arrange
        var firstName = _faker.Person.FirstName;

        // Act
        var result = _searchService.Search(firstName, null, null, null, null);

        // Assert
        result.ShouldNotBeNull();
        result.ShouldBeOfType<List<SearchResult>>();
    }

    [Fact]
    public void Search_WithOnlyLastName_ReturnsResults()
    {
        // Arrange
        var lastName = _faker.Person.LastName;

        // Act
        var result = _searchService.Search(null, lastName, null, null, null);

        // Assert
        result.ShouldNotBeNull();
        result.ShouldBeOfType<List<SearchResult>>();
    }

    [Fact]
    public void Search_WithOnlyDateOfBirth_ReturnsResults()
    {
        // Arrange
        var dateOfBirth = _faker.Date.Past(18);

        // Act
        var result = _searchService.Search(null, null, dateOfBirth, null, null);

        // Assert
        result.ShouldNotBeNull();
        result.ShouldBeOfType<List<SearchResult>>();
    }

    [Fact]
    public void Search_WithOnlyHouseNumberOrName_ReturnsResults()
    {
        // Arrange
        var houseNumberOrName = _faker.Address.BuildingNumber();

        // Act
        var result = _searchService.Search(null, null, null, houseNumberOrName, null);

        // Assert
        result.ShouldNotBeNull();
        result.ShouldBeOfType<List<SearchResult>>();
    }

    [Fact]
    public void Search_WithOnlyPostcode_ReturnsResults()
    {
        // Arrange
        var postcode = _faker.Address.ZipCode();

        // Act
        var result = _searchService.Search(null, null, null, null, postcode);

        // Assert
        result.ShouldNotBeNull();
        result.ShouldBeOfType<List<SearchResult>>();
    }

    [Fact]
    public void Search_WithAllNullParameters_ReturnsResults()
    {
        // Act
        var result = _searchService.Search(null, null, null, null, null);

        // Assert
        result.ShouldNotBeNull();
        result.ShouldBeOfType<List<SearchResult>>();
    }

    [Fact]
    public void Search_WithWhitespaceOnlyValues_ReturnsResults()
    {
        // Act
        var result = _searchService.Search("   ", "   ", null, "   ", "   ");

        // Assert
        result.ShouldNotBeNull();
        result.ShouldBeOfType<List<SearchResult>>();
    }

    [Theory]
    [InlineData("1234567890")]
    [InlineData("123 456 7890")]
    [InlineData("123-456-7890")]
    [InlineData("123.456.7890")]
    public void Search_WithVariousNhsNumberFormats_ReturnsResults(string nhsNumber)
    {
        // Act
        var result = _searchService.Search(nhsNumber);

        // Assert
        result.ShouldNotBeNull();
        result.ShouldBeOfType<List<SearchResult>>();
    }

    [Theory]
    [InlineData("SW1A 1AA")]
    [InlineData("sw1a1aa")]
    [InlineData("SW1A1AA")]
    [InlineData("  SW1A 1AA  ")]
    public void Search_WithVariousPostcodeFormats_ReturnsResults(string postcode)
    {
        // Arrange
        var firstName = _faker.Person.FirstName;

        // Act
        var result = _searchService.Search(firstName, null, null, null, postcode);

        // Assert
        result.ShouldNotBeNull();
        result.ShouldBeOfType<List<SearchResult>>();
    }

    [Fact]
    public void Search_WithMixedCaseNames_ReturnsResults()
    {
        // Arrange
        var firstName = _faker.Person.FirstName.ToUpper();
        var lastName = _faker.Person.LastName.ToLower();

        // Act
        var result = _searchService.Search(firstName, lastName, null, null, null);

        // Assert
        result.ShouldNotBeNull();
        result.ShouldBeOfType<List<SearchResult>>();
    }

    [Fact]
    public void Search_WithLeadingAndTrailingWhitespace_ReturnsResults()
    {
        // Arrange
        var firstName = $"  {_faker.Person.FirstName}  ";
        var lastName = $"  {_faker.Person.LastName}  ";
        var houseNumberOrName = $"  {_faker.Address.BuildingNumber()}  ";

        // Act
        var result = _searchService.Search(firstName, lastName, null, houseNumberOrName, null);

        // Assert
        result.ShouldNotBeNull();
        result.ShouldBeOfType<List<SearchResult>>();
    }

    [Fact]
    public void Search_WithSpecialCharactersInNames_ReturnsResults()
    {
        // Arrange
        const string firstName = "Mary-Jane";
        const string lastName = "O'Connor";

        // Act
        var result = _searchService.Search(firstName, lastName, null, null, null);

        // Assert
        result.ShouldNotBeNull();
        result.ShouldBeOfType<List<SearchResult>>();
    }

    [Fact]
    public void Search_WithVeryLongStrings_ReturnsResults()
    {
        // Arrange
        var firstName = new string('A', 100);
        var lastName = new string('B', 100);
        var houseNumberOrName = new string('C', 100);
        var postcode = new string('D', 20);

        // Act
        var result = _searchService.Search(firstName, lastName, null, houseNumberOrName, postcode);

        // Assert
        result.ShouldNotBeNull();
        result.ShouldBeOfType<List<SearchResult>>();
    }

    [Fact]
    public void Search_WithFutureDateOfBirth_ReturnsResults()
    {
        // Arrange
        var futureDate = DateTime.Now.AddYears(10);

        // Act
        var result = _searchService.Search(null, null, futureDate, null, null);

        // Assert
        result.ShouldNotBeNull();
        result.ShouldBeOfType<List<SearchResult>>();
    }

    [Fact]
    public void Search_WithVeryOldDateOfBirth_ReturnsResults()
    {
        // Arrange
        var oldDate = new DateTime(1900, 1, 1);

        // Act
        var result = _searchService.Search(null, null, oldDate, null, null);

        // Assert
        result.ShouldNotBeNull();
        result.ShouldBeOfType<List<SearchResult>>();
    }
}
