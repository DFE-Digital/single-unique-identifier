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
    [InlineData("12345678901234567")] // Too long
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
    [InlineData("1234567890123456")] // Valid Suid
    [InlineData("abcdefghijklmnop")] // Valid Suid
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
