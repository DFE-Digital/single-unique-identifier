using System.Threading;
using Bogus;
using Shouldly;
using SUI.SingleView.Application.Services;
using SUI.SingleView.Domain.Models;
using SUI.SingleView.Domain.UnitTests.Extensions;

namespace SUI.SingleView.Application.UnitTests.Services;

public class SearchServiceTests
{
    private readonly FakeDelay _delay = new();
    private readonly SearchService _searchService;
    private readonly Faker _faker = new("en_GB");

    public SearchServiceTests()
    {
        _searchService = new SearchService(_delay, TimeSpan.Zero);
    }

    [Fact]
    public async Task Search_WithNhsNumber_ReturnsResults()
    {
        // Arrange
        var nhsNumber = _faker.GenerateNhsNumber().Value;

        // Act
        var result = await _searchService.SearchAsync(nhsNumber);

        // Assert
        result.ShouldNotBeNull();
        result.ShouldBeOfType<List<SearchResult>>();
    }

    [Fact]
    public async Task Search_WithEmptyNhsNumber_ReturnsResults()
    {
        // Arrange
        var nhsNumber = string.Empty;

        // Act
        var result = await _searchService.SearchAsync(nhsNumber);

        // Assert
        result.ShouldNotBeNull();
        result.ShouldBeOfType<List<SearchResult>>();
    }

    [Fact]
    public async Task Search_WithNullNhsNumber_ReturnsResults()
    {
        // Arrange
        string nhsNumber = null!;

        // Act
        var result = await _searchService.SearchAsync(nhsNumber);

        // Assert
        result.ShouldNotBeNull();
        result.ShouldBeOfType<List<SearchResult>>();
    }

    [Fact]
    public async Task Search_WithPersonalDetails_ReturnsResults()
    {
        // Arrange
        var firstName = _faker.Person.FirstName;
        var lastName = _faker.Person.LastName;
        var dateOfBirth = _faker.Date.Past(18);
        var houseNumberOrName = _faker.Address.BuildingNumber();
        var postcode = _faker.Address.ZipCode();

        // Act
        var result = await _searchService.SearchAsync(
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
    public async Task Search_WithOnlyFirstName_ReturnsResults()
    {
        // Arrange
        var firstName = _faker.Person.FirstName;

        // Act
        var result = await _searchService.SearchAsync(firstName, null, null, null, null);

        // Assert
        result.ShouldNotBeNull();
        result.ShouldBeOfType<List<SearchResult>>();
    }

    [Fact]
    public async Task Search_WithOnlyLastName_ReturnsResults()
    {
        // Arrange
        var lastName = _faker.Person.LastName;

        // Act
        var result = await _searchService.SearchAsync(null, lastName, null, null, null);

        // Assert
        result.ShouldNotBeNull();
        result.ShouldBeOfType<List<SearchResult>>();
    }

    [Fact]
    public async Task Search_WithOnlyDateOfBirth_ReturnsResults()
    {
        // Arrange
        var dateOfBirth = _faker.Date.Past(18);

        // Act
        var result = await _searchService.SearchAsync(null, null, dateOfBirth, null, null);

        // Assert
        result.ShouldNotBeNull();
        result.ShouldBeOfType<List<SearchResult>>();
    }

    [Fact]
    public async Task Search_WithOnlyHouseNumberOrName_ReturnsResults()
    {
        // Arrange
        var houseNumberOrName = _faker.Address.BuildingNumber();

        // Act
        var result = await _searchService.SearchAsync(null, null, null, houseNumberOrName, null);

        // Assert
        result.ShouldNotBeNull();
        result.ShouldBeOfType<List<SearchResult>>();
    }

    [Fact]
    public async Task Search_WithOnlyPostcode_ReturnsResults()
    {
        // Arrange
        var postcode = _faker.Address.ZipCode();

        // Act
        var result = await _searchService.SearchAsync(null, null, null, null, postcode);

        // Assert
        result.ShouldNotBeNull();
        result.ShouldBeOfType<List<SearchResult>>();
    }

    [Fact]
    public async Task Search_WithAllNullParameters_ReturnsResults()
    {
        // Act
        var result = await _searchService.SearchAsync(null, null, null, null, null);

        // Assert
        result.ShouldNotBeNull();
        result.ShouldBeOfType<List<SearchResult>>();
    }

    [Fact]
    public async Task Search_WithWhitespaceOnlyValues_ReturnsResults()
    {
        // Act
        var result = await _searchService.SearchAsync("   ", "   ", null, "   ", "   ");

        // Assert
        result.ShouldNotBeNull();
        result.ShouldBeOfType<List<SearchResult>>();
    }

    [Theory]
    [InlineData("1234567890")]
    [InlineData("123 456 7890")]
    [InlineData("123-456-7890")]
    [InlineData("123.456.7890")]
    public async Task Search_WithVariousNhsNumberFormats_ReturnsResults(string nhsNumber)
    {
        // Act
        var result = await _searchService.SearchAsync(nhsNumber);

        // Assert
        result.ShouldNotBeNull();
        result.ShouldBeOfType<List<SearchResult>>();
    }

    [Theory]
    [InlineData("SW1A 1AA")]
    [InlineData("sw1a1aa")]
    [InlineData("SW1A1AA")]
    [InlineData("  SW1A 1AA  ")]
    public async Task Search_WithVariousPostcodeFormats_ReturnsResults(string postcode)
    {
        // Arrange
        var firstName = _faker.Person.FirstName;

        // Act
        var result = await _searchService.SearchAsync(firstName, null, null, null, postcode);

        // Assert
        result.ShouldNotBeNull();
        result.ShouldBeOfType<List<SearchResult>>();
    }

    [Fact]
    public async Task Search_WithMixedCaseNames_ReturnsResults()
    {
        // Arrange
        var firstName = _faker.Person.FirstName.ToUpper();
        var lastName = _faker.Person.LastName.ToLower();

        // Act
        var result = await _searchService.SearchAsync(firstName, lastName, null, null, null);

        // Assert
        result.ShouldNotBeNull();
        result.ShouldBeOfType<List<SearchResult>>();
    }

    [Fact]
    public async Task Search_WithLeadingAndTrailingWhitespace_ReturnsResults()
    {
        // Arrange
        var firstName = $"  {_faker.Person.FirstName}  ";
        var lastName = $"  {_faker.Person.LastName}  ";
        var houseNumberOrName = $"  {_faker.Address.BuildingNumber()}  ";

        // Act
        var result = await _searchService.SearchAsync(
            firstName,
            lastName,
            null,
            houseNumberOrName,
            null
        );

        // Assert
        result.ShouldNotBeNull();
        result.ShouldBeOfType<List<SearchResult>>();
    }

    [Fact]
    public async Task Search_WithSpecialCharactersInNames_ReturnsResults()
    {
        // Arrange
        const string firstName = "Mary-Jane";
        const string lastName = "O'Connor";

        // Act
        var result = await _searchService.SearchAsync(firstName, lastName, null, null, null);

        // Assert
        result.ShouldNotBeNull();
        result.ShouldBeOfType<List<SearchResult>>();
    }

    [Fact]
    public async Task Search_WithVeryLongStrings_ReturnsResults()
    {
        // Arrange
        var firstName = new string('A', 100);
        var lastName = new string('B', 100);
        var houseNumberOrName = new string('C', 100);
        var postcode = new string('D', 20);

        // Act
        var result = await _searchService.SearchAsync(
            firstName,
            lastName,
            null,
            houseNumberOrName,
            postcode
        );

        // Assert
        result.ShouldNotBeNull();
        result.ShouldBeOfType<List<SearchResult>>();
    }

    [Fact]
    public async Task Search_WithFutureDateOfBirth_ReturnsResults()
    {
        // Arrange
        var futureDate = DateTime.Now.AddYears(10);

        // Act
        var result = await _searchService.SearchAsync(null, null, futureDate, null, null);

        // Assert
        result.ShouldNotBeNull();
        result.ShouldBeOfType<List<SearchResult>>();
    }

    [Fact]
    public async Task Search_WithVeryOldDateOfBirth_ReturnsResults()
    {
        // Arrange
        var oldDate = new DateTime(1900, 1, 1);

        // Act
        var result = await _searchService.SearchAsync(null, null, oldDate, null, null);

        // Assert
        result.ShouldNotBeNull();
        result.ShouldBeOfType<List<SearchResult>>();
    }

    [Fact]
    public async Task Search_UsesConfiguredDelay()
    {
        // Arrange
        var configuredDelay = TimeSpan.FromSeconds(0.5);
        var searchService = new SearchService(_delay, configuredDelay);

        // Act
        await searchService.SearchAsync(null, null, null, null, null);

        // Assert
        _delay.CallCount.ShouldBe(1);
        _delay.LastDelay.ShouldBe(configuredDelay);
    }

    [Fact]
    public async Task Search_UsesDefaultDelay_WhenNotProvided()
    {
        // Arrange
        var fakeDelay = new FakeDelay();
        var searchService = new SearchService(fakeDelay);

        // Act
        await searchService.SearchAsync(null, null, null, null, null);

        // Assert
        fakeDelay.CallCount.ShouldBe(1);
        fakeDelay.LastDelay.ShouldBe(TimeSpan.FromSeconds(3));
    }

    private sealed class FakeDelay : IDelay
    {
        public int CallCount { get; private set; }
        public TimeSpan LastDelay { get; private set; } = TimeSpan.Zero;

        public Task DelayAsync(TimeSpan delay, CancellationToken cancellationToken = default)
        {
            CallCount++;
            LastDelay = delay;
            return Task.CompletedTask;
        }
    }
}
