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

    [Fact]
    public void DecryptPersonIdToNhs_WithWrongKey_ReturnsFailure()
    {
        // Arrange
        var logger = Substitute.For<ILogger<PersonIdEncryptionService>>();
        var service = new PersonIdEncryptionService(logger);

        var encryptionKey1 = Convert.ToBase64String(new byte[32]); // All zeros
        var encryptionKey2 = Convert.ToBase64String(Enumerable.Repeat((byte)1, 32).ToArray()); // All ones

        var encryption1 = new EncryptionDefinition
        {
            Algorithm = "AES-256-CBC",
            KeyId = "key-1",
            Key = encryptionKey1,
        };

        var encryption2 = new EncryptionDefinition
        {
            Algorithm = "AES-256-CBC",
            KeyId = "key-2",
            Key = encryptionKey2,
        };

        // Act - Encrypt with one key, decrypt with another
        var encryptResult = service.EncryptNhsToPersonId(Sui, encryption1);
        var decryptResult = service.DecryptPersonIdToNhs(encryptResult.Value!, encryption2);

        // Assert
        Assert.True(encryptResult.Success);
        Assert.False(decryptResult.Success);
        Assert.NotNull(decryptResult.Error);
        Assert.Contains("did not decrypt to expected", decryptResult.Error);
    }

    [Fact]
    public void EncryptNhsToPersonId_WithInvalidKeyLength_ReturnsFailure()
    {
        // Arrange
        var logger = Substitute.For<ILogger<PersonIdEncryptionService>>();
        var service = new PersonIdEncryptionService(logger);
        var encryption = new EncryptionDefinition
        {
            Algorithm = "AES-256-CBC",
            KeyId = "test-key-1",
            Key = Convert.ToBase64String(new byte[16]), // 16 bytes instead of 32
        };

        // Act
        var result = service.EncryptNhsToPersonId(Sui, encryption);

        // Assert
        Assert.False(result.Success);
        Assert.Null(result.Value);
        Assert.NotNull(result.Error);
        Assert.Contains("must be 32 bytes", result.Error);
    }

    [Fact]
    public void EncryptNhsToPersonId_WithUnsupportedAlgorithm_ReturnsFailure()
    {
        // Arrange
        var logger = Substitute.For<ILogger<PersonIdEncryptionService>>();
        var service = new PersonIdEncryptionService(logger);
        var encryption = new EncryptionDefinition
        {
            Algorithm = "AES-128-CBC", // Unsupported algorithm
            KeyId = "test-key-1",
            Key = Convert.ToBase64String(new byte[32]),
        };

        // Act
        var result = service.EncryptNhsToPersonId(Sui, encryption);

        // Assert
        Assert.False(result.Success);
        Assert.Null(result.Value);
        Assert.NotNull(result.Error);
        Assert.Contains("Unsupported algorithm", result.Error);
    }

    [Fact]
    public void DecryptPersonIdToNhs_WithInvalidKeyLength_ReturnsFailure()
    {
        // Arrange
        var logger = Substitute.For<ILogger<PersonIdEncryptionService>>();
        var service = new PersonIdEncryptionService(logger);
        var encryption = new EncryptionDefinition
        {
            Algorithm = "AES-256-CBC",
            KeyId = "test-key-1",
            Key = Convert.ToBase64String(new byte[16]), // 16 bytes instead of 32
        };

        // Act
        var result = service.DecryptPersonIdToNhs("someEncryptedValue", encryption);

        // Assert
        Assert.False(result.Success);
        Assert.Null(result.Value);
        Assert.NotNull(result.Error);
        Assert.Contains("must be 32 bytes", result.Error);
    }

    [Fact]
    public void DecryptPersonIdToNhs_WithUnsupportedAlgorithm_ReturnsFailure()
    {
        // Arrange
        var logger = Substitute.For<ILogger<PersonIdEncryptionService>>();
        var service = new PersonIdEncryptionService(logger);
        var encryption = new EncryptionDefinition
        {
            Algorithm = "AES-128-CBC", // Unsupported algorithm
            KeyId = "test-key-1",
            Key = Convert.ToBase64String(new byte[32]),
        };

        // Act
        var result = service.DecryptPersonIdToNhs("someEncryptedValue", encryption);

        // Assert
        Assert.False(result.Success);
        Assert.Null(result.Value);
        Assert.NotNull(result.Error);
        Assert.Contains("Unsupported algorithm", result.Error);
    }
}
