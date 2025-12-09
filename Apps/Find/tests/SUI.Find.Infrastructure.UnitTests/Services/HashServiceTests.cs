using SUI.Find.Infrastructure.Services;

namespace SUI.Find.Infrastructure.UnitTests.Services;

public class HashServiceTests
{
    private readonly HashService _sut = new HashService();
    
    [Fact]
    public void HashInput_ShouldBeDeterministic_ForSameInput()
    {
        // Arrange
        const string input = "test-client-id";

        // Act
        var hash1 = _sut.HmacSha256Hash(input);
        var hash2 = _sut.HmacSha256Hash(input);

        // Assert
        Assert.NotNull(hash1);
        Assert.NotEmpty(hash1);
        Assert.Equal(hash1, hash2);
    }

    [Fact]
    public void HashInput_ShouldReturnDifferentHashes_ForDifferentInputs()
    {
        // Arrange
        const string input1 = "test-client-id-a";
        const string input2 = "test-client-id-b";

        // Act
        var hash1 = _sut.HmacSha256Hash(input1);
        var hash2 = _sut.HmacSha256Hash(input2);

        // Assert
        Assert.NotEqual(hash1, hash2);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void HashInput_ShouldReturnEmptyString_WhenInputIsNullOrEmpty(string? input)
    {
        // Act
        var result = _sut.HmacSha256Hash(input!);

        // Assert
        Assert.Equal(string.Empty, result);
    }
}