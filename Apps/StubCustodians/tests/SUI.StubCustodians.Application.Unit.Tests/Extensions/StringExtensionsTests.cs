using SUI.StubCustodians.Application.Extensions;

namespace SUI.StubCustodians.Application.Unit.Tests.Extensions;

public class StringExtensionsTests
{
    [Theory]
    [InlineData("example", "example/")]
    [InlineData("example/", "example/")]
    [InlineData("example//", "example/")]
    public void EnsureTrailingSlash_Tests(string input, string expected) =>
        Assert.Equal(expected, input.EnsureTrailingSlash());
}
