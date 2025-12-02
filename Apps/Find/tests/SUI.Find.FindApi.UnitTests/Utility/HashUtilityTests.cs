using SUI.Find.FindApi.Utility;

namespace SUI.Find.FindApi.UnitTests.Utility;

public class HashUtilityTests
{
    [Fact]
    public void HashInput_ShouldBeDeterministic_ForSameInput()
    {
        // Arrange
        var input = "test-client-id";

        // Act
        var hash1 = HashUtility.HashInput(input);
        var hash2 = HashUtility.HashInput(input);

        // Assert
        Assert.NotNull(hash1);
        Assert.NotEmpty(hash1);
        Assert.Equal(hash1, hash2);
    }

    [Fact]
    public void HashInput_ShouldReturnDifferentHashes_ForDifferentInputs()
    {
        // Arrange
        var input1 = "test-client-id-a";
        var input2 = "test-client-id-b";

        // Act
        var hash1 = HashUtility.HashInput(input1);
        var hash2 = HashUtility.HashInput(input2);

        // Assert
        Assert.NotEqual(hash1, hash2);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void HashInput_ShouldReturnEmptyString_WhenInputIsNullOrEmpty(string? input)
    {
        // Act
        var result = HashUtility.HashInput(input);

        // Assert
        Assert.Equal(string.Empty, result);
    }
}