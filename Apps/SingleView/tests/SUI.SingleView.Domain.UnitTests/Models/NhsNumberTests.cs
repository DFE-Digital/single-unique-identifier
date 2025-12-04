using Shouldly;
using SUI.SingleView.Domain.Models;

namespace SUI.SingleView.Domain.UnitTests.Models;

public class NhsNumberTests
{
    [Theory]
    [InlineData("1111111111", "111 111 1111")]
    [InlineData("2222222222", "222 222 2222")]
    [InlineData("3333333333", "333 333 3333")]
    [InlineData("4444444444", "444 444 4444")]
    [InlineData("5555555555", "555 555 5555")]
    public void ToString_FormatsNhsNumberCorrectly(string validNhsNumber, string expected)
    {
        // Arrange
        var sut = NhsNumber.Parse(validNhsNumber);

        // Act
        var result = sut.ToString();

        // Assert
        result.ShouldBe(expected);
    }

    [Fact]
    public void TryParse_WithBlankString_ReturnsFalse()
    {
        // Act
        var sut = NhsNumber.TryParse("", out _);

        // Assert
        sut.ShouldBeFalse();
    }

    [Theory]
    [InlineData("123456789")]
    [InlineData("12345678910")]
    public void TryParse_WithInvalidLength_ReturnsFalse(string invalidNhsNumber)
    {
        // Act
        var sut = NhsNumber.TryParse(invalidNhsNumber, out _);

        // Assert
        sut.ShouldBeFalse();
    }

    [Fact]
    public void TryParse_WithInvalidChecksum_ReturnsFalse()
    {
        // Act
        var sut = NhsNumber.TryParse("1111111112", out _);

        // Assert
        sut.ShouldBeFalse();
    }

    [Fact]
    public void TryParse_With10Checksum_ReturnsFalse()
    {
        // Act
        var sut = NhsNumber.TryParse("1000000010", out _);

        // Assert
        sut.ShouldBeFalse();
    }

    [Theory]
    [InlineData("1111111111")]
    [InlineData("2222222222")]
    [InlineData("3333333333")]
    [InlineData("4444444444")]
    public void TryParse_WithValidNhsNumber_ReturnsTrueAndNhsNumber(string validNhsNumber)
    {
        // Act
        var sut = NhsNumber.TryParse(validNhsNumber, out var result);

        // Assert
        sut.ShouldBeTrue();
        result.ShouldBeEquivalentTo(NhsNumber.Parse(validNhsNumber));
    }

    [Fact]
    public void Parse_WithInvalidNhsNumber_ThrowsException()
    {
        // Arrange
        var invalidNhsNumber = "12345"; // Invalid length and format

        // Act / Assert
        var exception = Should.Throw<FormatException>(() => NhsNumber.Parse(invalidNhsNumber));
        exception.Message.ShouldBe("Invalid NHS number.");
    }

    [Fact]
    public void Implicit_ToString_ReturnsNhsNumber()
    {
        // Arrange
        var nhsNumber = NhsNumber.Parse("1111111111");

        // Act
        string value = nhsNumber;

        // Assert
        value.ShouldBe("1111111111");
    }

    [Fact]
    public void Implicit_FromString_ParsesCorrectly()
    {
        // Arrange
        const string input = "111 111 1111";

        // Act
        NhsNumber nhsNumber = input;

        // Assert
        nhsNumber.Value.ShouldBe("1111111111");
    }
}
