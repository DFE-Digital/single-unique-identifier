using Microsoft.Extensions.Logging;
using NSubstitute;
using SUI.Find.Domain.Models;
using SUI.Find.Infrastructure.Services;

namespace SUI.Find.Infrastructure.UnitTests.Services;

public class PersonIdEncryptionServiceTests
{
    private const string Sui = "1234567890";

    [Fact]
    public void EncryptNhsToPersonId_WithValidNhs_ReturnsEncryptedPersonId()
    {
        // Arrange
        var logger = Substitute.For<ILogger<PersonIdEncryptionService>>();
        var service = new PersonIdEncryptionService(logger);
        var encryption = new EncryptionDefinition
        {
            Algorithm = "AES-256-CBC",
            KeyId = "test-key-1",
            Key = Convert.ToBase64String(new byte[32]),
        };

        // Act
        var result = service.EncryptNhsToPersonId(Sui, encryption);

        // Assert
        Assert.True(result.Success);
        Assert.NotNull(result.Value);
        Assert.False(string.IsNullOrEmpty(result.Value));
        Assert.NotEqual(Sui, result.Value);
    }

    [Fact]
    public void DecryptPersonIdToNhs_WithValidPersonId_ReturnsOriginalNhs()
    {
        // Arrange
        var logger = Substitute.For<ILogger<PersonIdEncryptionService>>();
        var service = new PersonIdEncryptionService(logger);
        var encryption = new EncryptionDefinition
        {
            Algorithm = "AES-256-CBC",
            KeyId = "test-key-1",
            Key = Convert.ToBase64String(new byte[32]),
        };

        // Act
        var encryptResult = service.EncryptNhsToPersonId(Sui, encryption);
        var decryptResult = service.DecryptPersonIdToNhs(encryptResult.Value!, encryption);

        // Assert
        Assert.True(encryptResult.Success);
        Assert.True(decryptResult.Success);
        Assert.Equal(Sui, decryptResult.Value);
    }

    [Fact]
    public void EncryptNhsToPersonId_WithInvalidKey_ReturnsFailure()
    {
        // Arrange
        var logger = Substitute.For<ILogger<PersonIdEncryptionService>>();
        var service = new PersonIdEncryptionService(logger);
        var encryption = new EncryptionDefinition
        {
            Algorithm = "AES-256-CBC",
            KeyId = "test-key-1",
            Key = "invalid-base64-key!!!", // Invalid base64
        };

        // Act
        var result = service.EncryptNhsToPersonId(Sui, encryption);

        // Assert
        Assert.False(result.Success);
        Assert.Null(result.Value);
        Assert.NotNull(result.Error);
        Assert.Contains("Invalid encryption key format", result.Error);
    }
}
