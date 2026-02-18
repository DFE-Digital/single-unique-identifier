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
            false,
            out var errorMessage
        );

        // Assert
        Assert.False(isValid);
        Assert.NotNull(errorMessage);
    }

    [Theory]
    [InlineData("Cy14hyZL-4LSIwVy50p-H", true)] // Too short
    [InlineData("Cy13hyZL-4LSIwVy50p-Hgglsa", true)] // Too long
    [InlineData("1234567890123", false)] // Too long
    [InlineData("123456789", false)] // Too short
    public void ShouldReturnFalse_WhenSuidIsInvalid(string? suid, bool encryptedIds)
    {
        // Arrange
        // Act
        var isValid = Validators.StartSearchRequestValidator.IsValid(
            new StartSearchRequest(suid!),
            encryptedIds,
            out var errorMessage
        );

        // Assert
        Assert.False(isValid);
        Assert.NotNull(errorMessage);
    }

    [Theory]
    [InlineData("Cy13hyZL-4LSIwVy50p-Hg", true)] // Valid Suid
    [InlineData("Cy13hyZL-4LSIwVy50l-Hg", true)] // Valid Suid
    [InlineData("9691292211", false)] // Valid Suid
    public void ShouldReturnTrue_WhenSuidIsValid(string suid, bool encryptedIds)
    {
        // Arrange
        // Act
        var isValid = Validators.StartSearchRequestValidator.IsValid(
            new StartSearchRequest(suid),
            encryptedIds,
            out var errorMessage
        );

        // Assert
        Assert.True(isValid);
        Assert.True(string.IsNullOrEmpty(errorMessage));
    }
}
