using SUI.Find.Domain.ValueObjects;

namespace SUI.Find.Domain.UnitTests.ValueObjects;

public class NhsPersonIdTests
{
    [Theory]
    [InlineData("9876543210", "9876543210")]
    [InlineData("987 654 3210", "9876543210")]
    public void Should_Succeed_When_NumberIsValid_And_ValueIsCleaned(string input, string expected)
    {
        var result = NhsPersonId.Create(input);

        Assert.True(result.Success);
        Assert.NotNull(result.Value);
        Assert.Equal(expected, result.Value.Value);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Should_Fail_When_ValueIsNullOrWhitespace(string? input)
    {
        var result = NhsPersonId.Create(input!);

        Assert.False(result.Success);
    }

    [Theory]
    [InlineData("123456789")]
    [InlineData("12345678901")]
    [InlineData("12 34 567")]
    public void Should_Fail_When_LengthIsNotTenAfterCleaning(string input)
    {
        var result = NhsPersonId.Create(input);

        Assert.False(result.Success);
    }

    [Theory]
    [InlineData("123456789O")]
    [InlineData("A234567890")]
    [InlineData("12345-7890")]
    public void Should_Fail_When_ValueContainsNonDigits(string input)
    {
        var result = NhsPersonId.Create(input);

        Assert.False(result.Success);
    }

    [Theory]
    [InlineData("1234567890")] // expected check digit would be 10 (invalid)
    [InlineData("9876543211")] // wrong check digit (valid is 0)
    public void Should_Fail_When_ChecksumIsInvalid(string input)
    {
        var result = NhsPersonId.Create(input);

        Assert.False(result.Success);
    }
}
