using SUI.AuthEmulator.Services;

namespace SUI.AuthEmulator.UnitTests.Services;

public class JwksKeyProviderTests
{
    [Fact]
    public void Constructor_ShouldInitializeFourRsaKeys()
    {
        // Act
        var provider = new JwksKeyProvider();
        var keys = provider.GetKeys();

        // Assert
        Assert.NotNull(keys);
        Assert.Equal(4, keys.Count);
    }

    [Fact]
    public void Keys_ShouldHaveDistinctAndFormattedKeyIds()
    {
        // Arrange & Act
        var provider = new JwksKeyProvider();
        var keys = provider.GetKeys().ToList();

        // Assert
        var keyIds = keys.Select(k => k.Key.KeyId).ToList();

        // Ensure all 4 IDs are completely unique
        Assert.Equal(4, keyIds.Distinct().Count());

        // Verify suffix matches the indexing strategy (0 to 3)
        for (int i = 0; i < 4; i++)
        {
            Assert.EndsWith($"_{i}", keys[i].Key.KeyId);
        }
    }

    [Fact]
    public void Keys_ShouldContainValidPublicParameters()
    {
        // Arrange & Act
        var provider = new JwksKeyProvider();
        var keys = provider.GetKeys();

        // Assert
        foreach (var keyDetails in keys)
        {
            Assert.NotNull(keyDetails.Key);
            Assert.NotNull(keyDetails.SigningCredentials);

            // Public Modulus (n) and Exponent (e) must be populated for JWKS
            Assert.False(string.IsNullOrWhiteSpace(keyDetails.Modulus));
            Assert.False(string.IsNullOrWhiteSpace(keyDetails.Exponent));

            // Confirm the algorithm is explicitly RS256
            Assert.Equal("RS256", keyDetails.SigningCredentials.Algorithm);
        }
    }
}
