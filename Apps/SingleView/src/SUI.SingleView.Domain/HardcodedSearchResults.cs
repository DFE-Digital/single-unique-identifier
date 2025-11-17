using System.Diagnostics.CodeAnalysis;
using SUI.SingleView.Domain.Models;

namespace SUI.SingleView.Domain;

[ExcludeFromCodeCoverage]
public static class HardCodedSearchResults
{
    public static readonly IReadOnlyList<SearchResult> All = new[]
    {
        new SearchResult
        {
            NhsNumber = NhsNumber.Parse("0000000000"),
            Name = "Alex Zero",
            DateOfBirth = new DateTime(2020, 1, 1),
            Address = new Address
            {
                AddressLine1 = "0 Test Street",
                Town = "Testford",
                County = "Testshire",
                Postcode = "TE0 0ST",
                Country = "England",
            },
        },
        new SearchResult
        {
            NhsNumber = NhsNumber.Parse("1111111111"),
            Name = "Alex One",
            DateOfBirth = new DateTime(2019, 2, 1),
            Address = new Address
            {
                AddressLine1 = "1 Sample Road",
                Town = "Sampleton",
                County = "Sampleshire",
                Postcode = "SA1 1PL",
                Country = "England",
            },
        },
        new SearchResult
        {
            NhsNumber = NhsNumber.Parse("2222222222"),
            Name = "Bob Two",
            DateOfBirth = new DateTime(2018, 3, 1),
            Address = new Address
            {
                AddressLine1 = "2 Example Avenue",
                Town = "Exampleford",
                County = "Exampleshire",
                Postcode = "EX2 2MP",
                Country = "England",
            },
        },
        new SearchResult
        {
            NhsNumber = NhsNumber.Parse("3333333333"),
            Name = "Charlie Three",
            DateOfBirth = new DateTime(2017, 4, 1),
            Address = new Address
            {
                AddressLine1 = "3 Demo Close",
                Town = "Demoton",
                County = "Demoshire",
                Postcode = "DE3 3MO",
                Country = "England",
            },
        },
        new SearchResult
        {
            NhsNumber = NhsNumber.Parse("4444444444"),
            Name = "Diana Four",
            DateOfBirth = new DateTime(2016, 5, 1),
            Address = new Address
            {
                AddressLine1 = "4 Mock Lane",
                Town = "Mockford",
                County = "Mockshire",
                Postcode = "MO4 4CK",
                Country = "England",
            },
        },
        new SearchResult
        {
            NhsNumber = NhsNumber.Parse("5555555555"),
            Name = "Evan Five",
            DateOfBirth = new DateTime(2015, 6, 1),
            Address = new Address
            {
                AddressLine1 = "5 Fixture Way",
                Town = "Fixtureham",
                County = "Fixtureshire",
                Postcode = "FI5 5XT",
                Country = "England",
            },
        },
        new SearchResult
        {
            NhsNumber = NhsNumber.Parse("6666666666"),
            Name = "Freya Six",
            DateOfBirth = new DateTime(2014, 7, 1),
            Address = new Address
            {
                AddressLine1 = "6 Stub Street",
                Town = "Stubton",
                County = "Stubshire",
                Postcode = "ST6 6UB",
                Country = "England",
            },
        },
        new SearchResult
        {
            NhsNumber = NhsNumber.Parse("7777777777"),
            Name = "George Seven",
            DateOfBirth = new DateTime(2013, 8, 1),
            Address = new Address
            {
                AddressLine1 = "7 Dummy Drive",
                Town = "Dummyford",
                County = "Dummyshire",
                Postcode = "DU7 7MY",
                Country = "England",
            },
        },
        new SearchResult
        {
            NhsNumber = NhsNumber.Parse("8888888888"),
            Name = "Hannah Eight",
            DateOfBirth = new DateTime(2012, 9, 1),
            Address = new Address
            {
                AddressLine1 = "8 Placeholder Place",
                Town = "Placeville",
                County = "Placehire",
                Postcode = "PL8 8CE",
                Country = "England",
            },
        },
        new SearchResult
        {
            NhsNumber = NhsNumber.Parse("9999999999"),
            Name = "Isaac Nine",
            DateOfBirth = new DateTime(2011, 10, 1),
            Address = new Address
            {
                AddressLine1 = "9 Sample Street",
                Town = "Ninebridge",
                County = "Nineshire",
                Postcode = "NI9 9NE",
                Country = "England",
            },
        },
    }.ToList().AsReadOnly();
}
