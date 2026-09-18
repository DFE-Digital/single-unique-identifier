using SUI.NotificationService.Infrastructure.Utilities;

namespace SUI.NotificationService.Infrastructure.UnitTests.Utilities;

public class TableKeyNormaliserTests
{
    [Theory]
    [InlineData("supplier-one", "SUPPLIER-ONE")] // Uppercase conversion
    [InlineData("SUPPLIER-TWO", "SUPPLIER-TWO")] // Already uppercase
    [InlineData("sub/org#name?1\\2", "SUB_ORG_NAME_1_2")] // Strips invalid table chars: / \ # ?
    [InlineData("   spaced   ", "SPACED")] // Trims padding
    public void Normalise_TransformsStringCorrectly(string input, string expected)
    {
        var result = TableKeyNormaliser.Normalise(input);
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")] // Tests the IsNullOrWhiteSpace condition
    public void Normalise_NullOrWhiteSpace_ThrowsArgumentException(string? invalidInput)
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() =>
            TableKeyNormaliser.Normalise(invalidInput!)
        );

        // Asserting the exact message ensures nobody accidentally changes the validation
        Assert.Equal("Key value cannot be null or empty.", exception.Message);
    }
}
