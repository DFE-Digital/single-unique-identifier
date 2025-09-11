using System.Diagnostics;
using System.Globalization;
using SUi.Find.Application.Interfaces;
using SUi.Find.Application.Models;
using SUI.Find.Infrastructure.Constants;
using SUI.Find.Infrastructure.Services;

namespace SUI.Find.Infrastructure.UnitTests.Utilities;

public class SearchIdServiceTests
{
    private readonly SearchIdService _searchIdService = new();
    
    [Fact]
    public void CreatePersonHash_ReturnsConsistentHash_ForSameInput()
    {
        var person = new PersonSpecification
        {
            Given = "John",
            Family = "Doe",
            Gender = "male",
            BirthDate = new DateOnly(1990, 1, 1),
            AddressPostalCode = "AB1 2CD"
        };

        var hash1 = _searchIdService.CreatePersonHash(
            person.Given, person.Family, person.BirthDate.GetValueOrDefault(),person.Gender, person.AddressPostalCode);
        var hash2 = "be916a1b542b16fb6f8df9b9f593959d64481bd4ac2a9fcd6ac26b575dad3ab3";
        Assert.False(string.IsNullOrWhiteSpace(hash1.Value));
        Assert.Equal(hash1.Value, hash2);
    }

    [Fact]
    public void CreatePersonHash_ChangesHash_WhenRelevantFieldChanges()
    {
        var person1 = new PersonSpecification
        {
            Given = "John",
            Family = "Doe",
            Gender = "male",
            BirthDate = new DateOnly(1990, 1, 1),
            AddressPostalCode = "AB1 2CD"
        };

        var person2 = new PersonSpecification
        {
            Given = "john",
            Family = "doe",
            Gender = "male",
            BirthDate = new DateOnly(1990, 1, 1),
            AddressPostalCode = "AB1 2CD"
        };

        var hash1 = _searchIdService.CreatePersonHash(
            person1.Given, person1.Family, person1.BirthDate.GetValueOrDefault(),person1.Gender,  person1.AddressPostalCode);
        var hash2 = _searchIdService.CreatePersonHash(
            person2.Given, person2.Family,  person2.BirthDate.GetValueOrDefault(), person2.Gender,person2.AddressPostalCode);

        Assert.Equal(hash1, hash2);
    }

    [Theory]
    [InlineData("male")]
    [InlineData("1")] // Dbs gender code is "1" for "male"
    [InlineData("")]
    [InlineData(null)]
    public void CreatePersonHash_HandlesGenderVariants(string? genderInput)
    {
        var person = new PersonSpecification
        {
            Given = "John",
            Family = "Doe",
            Gender = genderInput,
            BirthDate = new DateOnly(1990, 1, 1),
            AddressPostalCode = "AB1 2CD"
        };

        var hash = _searchIdService.CreatePersonHash(
            person.Given, person.Family, person.BirthDate.GetValueOrDefault(),person.Gender, person.AddressPostalCode);

        Assert.False(string.IsNullOrWhiteSpace(hash.Value));
    }

    [Fact]
    public void CreatePersonHash_HandlesPostalCodeCasingAndWhitespace()
    {
        var person1 = new PersonSpecification
        {
            Given = "John",
            Family = "Doe",
            Gender = "male",
            BirthDate = new DateOnly(1990, 1, 1),
            AddressPostalCode = "AB1 2CD"
        };

        var person2 = new PersonSpecification
        {
            Given = "John",
            Family = "Doe",
            Gender = "male",
            BirthDate = new DateOnly(1990, 1, 1),
            AddressPostalCode = " ab1   2cd "
        };

        var hash1 = _searchIdService.CreatePersonHash(
            person1.Given, person1.Family, person1.BirthDate.GetValueOrDefault(),person1.Gender,  person1.AddressPostalCode);
        var hash2 = _searchIdService.CreatePersonHash(
            person2.Given, person2.Family,  person2.BirthDate.GetValueOrDefault(), person2.Gender,person2.AddressPostalCode);

        Assert.Equal(hash1, hash2);
    }

    [Fact]
    public void CreatePersonHash_HandlesDifferentDateInputs()
    {
        var person1 = new PersonSpecification
        {
            Given = "John",
            Family = "Doe",
            Gender = "male",
            BirthDate = DateOnly.Parse("October 21, 2015", CultureInfo.InvariantCulture),
            AddressPostalCode = "AB1 2CD"
        };

        var person2 = new PersonSpecification
        {
            Given = "John",
            Family = "Doe",
            Gender = "male",
            BirthDate = new DateOnly(2015, 10, 21),
            AddressPostalCode = " ab1   2cd "
        };

        var hash1 = _searchIdService.CreatePersonHash(
            person1.Given, person1.Family, person1.BirthDate.GetValueOrDefault(),person1.Gender,  person1.AddressPostalCode);
        var hash2 = _searchIdService.CreatePersonHash(
            person2.Given, person2.Family,  person2.BirthDate.GetValueOrDefault(), person2.Gender,person2.AddressPostalCode);

        Assert.Equal(hash1, hash2);
    }

    [Fact]
    public void StoreSearchIdInBaggage_StoresHashInActivityBaggage()
    {
        
        Activity.Current = new Activity("TestActivity").Start();
        var hash = new SearchIdHash("testhashvalue");
        _searchIdService.StoreSearchIdInBaggage(hash);
        var baggageValue = Activity.Current.Baggage
            .FirstOrDefault(kv => kv.Key == SearchIdConstants.SearchIdStorageKey).Value;
        Assert.Equal(hash.Value, baggageValue);
        
        // Clean up
        Activity.Current.Stop();
        Activity.Current = null;
    }
}