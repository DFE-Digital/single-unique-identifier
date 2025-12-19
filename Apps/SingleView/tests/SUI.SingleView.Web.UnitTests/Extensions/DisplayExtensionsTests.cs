using Shouldly;
using SUI.SingleView.Domain.Models;
using SUI.SingleView.Web.Extensions;
using SUI.Transfer.API.Client;

namespace SUI.SingleView.Web.UnitTests.Extensions;

public class DisplayExtensionsTests
{
    [Theory]
    [InlineData(true, "Yes")]
    [InlineData(false, "No")]
    [InlineData(null, "")]
    public void ToYesNo_Tests(bool? input, string expectedResult)
    {
        input.ToYesNo().ShouldBe(expectedResult);
    }

    [Fact]
    public void ToAddress_WhenAddressIsNull_Returns_NoKnownAddress()
    {
        AddressV1? input = null;

        // ACT
        var result = input.ToAddress();

        // ASSERT
        result.ShouldBe(new Address { AddressLine1 = "No known address" });
    }

    [Fact]
    public void ToAddress_FullyPopulatedInput_Scenario()
    {
        AddressV1? input = new()
        {
            Line1 = "Line 1",
            Line2 = "Line 2",
            TownOrCity = "Town or City",
            County = "County",
            Postcode = "PCode",
        };

        // ACT
        var result = input.ToAddress();

        // ASSERT
        result.ShouldBe(
            new Address
            {
                AddressLine1 = "Line 1",
                AddressLine2 = "Line 2",
                Town = "Town or City",
                County = "County",
                Postcode = "PCode",
            }
        );
    }

    [Fact]
    public void ToAddress_PartiallyPopulatedInput_Scenario()
    {
        AddressV1? input = new() { Line2 = "Line 2", Postcode = "PCode" };

        // ACT
        var result = input.ToAddress();

        // ASSERT
        result.ShouldBe(
            new Address
            {
                AddressLine1 = "",
                AddressLine2 = "Line 2",
                Postcode = "PCode",
            }
        );
    }
}
