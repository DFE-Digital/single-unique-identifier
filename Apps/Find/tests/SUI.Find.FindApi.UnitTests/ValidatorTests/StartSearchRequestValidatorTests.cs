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
    [InlineData("1234567890123")] // Too long
    [InlineData("123456789")] // Too short
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
    [InlineData("9691292211")] // Valid Suid
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
        Assert.True(string.IsNullOrEmpty(errorMessage));
    }
}
