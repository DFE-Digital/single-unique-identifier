namespace SUI.Shared.UnitTests;

public sealed class NhsNumberValidatorTests
{
    [Theory]
    [InlineData("9000000009")]
    [InlineData("9000000017")]
    [InlineData("9876543210")]
    [InlineData("9434765919")]
    [InlineData("9434765870")] // remainder 0, so the check digit is 0
    public void IsValid_ReturnsTrue_WhenValueIsAValidNhsNumber(string value)
    {
        Assert.True(NhsNumberValidator.IsValid(value));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void IsValid_ReturnsFalse_WhenValueIsMissing(string? value)
    {
        Assert.False(NhsNumberValidator.IsValid(value));
    }

    [Theory]
    [InlineData("900000000")]
    [InlineData("90000000090")]
    [InlineData("9")]
    public void IsValid_ReturnsFalse_WhenValueIsNotTenCharacters(string value)
    {
        Assert.False(NhsNumberValidator.IsValid(value));
    }

    [Theory]
    [InlineData("900000000O")]
    [InlineData("A000000009")]
    [InlineData("900-000-09")]
    [InlineData("900 000 09")]
    public void IsValid_ReturnsFalse_WhenValueIsNotNumeric(string value)
    {
        Assert.False(NhsNumberValidator.IsValid(value));
    }

    [Theory]
    [InlineData("900 000 0009")]
    [InlineData(" 9000000009")]
    [InlineData("9000000009 ")]
    public void IsValid_ReturnsFalse_WhenValueIsFormatted(string value)
    {
        Assert.False(NhsNumberValidator.IsValid(value));
    }

    [Fact]
    public void IsValid_ReturnsFalse_WhenValueStartsWithZero()
    {
        // Checksum-valid, so only the leading zero rule rejects it.
        Assert.False(NhsNumberValidator.IsValid("0000000000"));
    }

    [Theory]
    [InlineData("9000000008")] // wrong check digit (valid is 9)
    [InlineData("9876543211")] // wrong check digit (valid is 0)
    [InlineData("1234567890")] // check digit would be 10, which is never issued
    public void IsValid_ReturnsFalse_WhenChecksumIsInvalid(string value)
    {
        Assert.False(NhsNumberValidator.IsValid(value));
    }
}
