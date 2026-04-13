namespace SUI.Find.Infrastructure.UnitTests
{
    public class TableKeyNormaliserTests
    {
        [Theory]
        [InlineData("abc", "ABC")] // Uppercase
        [InlineData(" abc ", "ABC")] // Trim + Uppercase
        [InlineData("a/b\\c#d?e", "A_B_C_D_E")] // Forbidden character replacement
        [InlineData("   a/b  ", "A_B")] // Trim + replace
        public void Normalise_ShouldReturnExpectedValue(string input, string expected)
        {
            var result = TableKeyNormaliser.Normalise(input);
            Assert.Equal(expected, result);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void Normalise_ShouldThrowArgumentException_WhenInputIsNullOrWhiteSpace(
            string? input
        )
        {
            var ex = Assert.Throws<ArgumentException>(() => TableKeyNormaliser.Normalise(input!));
            Assert.Equal("Key value cannot be null or empty.", ex.Message);
        }
    }
}
