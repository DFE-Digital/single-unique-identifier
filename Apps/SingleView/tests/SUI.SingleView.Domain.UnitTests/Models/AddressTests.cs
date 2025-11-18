using Shouldly;
using SUI.SingleView.Domain.Models;
using SUI.SingleView.Domain.UnitTests.Fakers;

namespace SUI.SingleView.Domain.UnitTests.Models;

public class AddressTests
{
    [Fact]
    public void ToSingleLine_ReturnsAddressAsSingleLine()
    {
        var sut = new Address()
        {
            AddressLine1 = "10 Test street",
            AddressLine2 = "Apt 1",
            Town = "Test City",
            County = "Test County",
            Country = "Test Country",
            Postcode = "TE5T T1NG",
        };

        var result = sut.ToSingleLine();

        result.ShouldBe("10 Test street, Apt 1, Test City, Test County, TE5T T1NG");
    }
}
