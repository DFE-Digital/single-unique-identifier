using SUI.Find.FindApi.Models;

namespace SUI.Find.FindApi.UnitTests.ValidatorTests;

public class StartSearchRequestValidatorTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void ShouldReturnFalse_WhenModelSuidIsNullOrEmpty(string? suid)
    {
        // Arrange
        // Act
        var isValid = Validators.StartSearchRequestValidator.IsValid(
            new StartSearchRequest(suid!),
            out var errorMessage
        );

        // Assert
        Assert.False(isValid);
        Assert.NotNull(errorMessage);
    }

    [Theory]
    [InlineData("123456789")] // Too short
    [InlineData("12345678901")] // Too long
    [InlineData("12345ABCDE")] // Non-numeric
    [InlineData("1234567890")] // Invalid checksum
    public void ShouldReturnFalse_WhenSuidIsInvalid(string? suid)
    {
        // Arrange
        // Act
        var isValid = Validators.StartSearchRequestValidator.IsValid(
            new StartSearchRequest(suid!),
            out var errorMessage
        );

        // Assert
        Assert.False(isValid);
        Assert.NotNull(errorMessage);
    }

    [Theory]
    [InlineData("9434765870")] // Valid NHS number
    [InlineData(" 943 476 5870 ")] // Valid NHS number with spaces
    public void ShouldReturnTrue_WhenSuidIsValid(string suid)
    {
        // Arrange
        // Act
        var isValid = Validators.StartSearchRequestValidator.IsValid(
            new StartSearchRequest(suid),
            out var errorMessage
        );

        // Assert
        Assert.True(isValid);
        Assert.Null(errorMessage);
    }
}
