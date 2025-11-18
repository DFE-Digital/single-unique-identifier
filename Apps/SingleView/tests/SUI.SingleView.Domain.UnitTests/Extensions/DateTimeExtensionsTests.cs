using Shouldly;
using SUI.SingleView.Domain.Extensions;

namespace SUI.SingleView.Domain.UnitTests.Extensions;

public class DateTimeExtensionsTests
{
    [Fact]
    public void ToAgeInYearsString_WithFutureBirthDate_ReturnsZeroYearsOld()
    {
        // Arrange
        var futureDate = DateTime.Today.AddDays(1);

        // Act
        var result = futureDate.ToAgeInYearsString();

        // Assert
        result.ShouldBe("0 years old");
    }

    [Fact]
    public void ToAgeInYearsString_WithBirthDateToday_ReturnsZeroYearsOld()
    {
        // Arrange
        var today = DateTime.Today;

        // Act
        var result = today.ToAgeInYearsString();

        // Assert
        result.ShouldBe("0 years old");
    }

    [Fact]
    public void ToAgeInYearsString_WithBirthDateYesterday_ReturnsZeroYearsOld()
    {
        // Arrange
        var yesterday = DateTime.Today.AddDays(-1);

        // Act
        var result = yesterday.ToAgeInYearsString();

        // Assert
        result.ShouldBe("0 years old");
    }

    [Fact]
    public void ToAgeInYearsString_WithBirthDateOneYearAgoToday_ReturnsOneYearOld()
    {
        // Arrange
        var oneYearAgoToday = DateTime.Today.AddYears(-1);

        // Act
        var result = oneYearAgoToday.ToAgeInYearsString();

        // Assert
        result.ShouldBe("1 years old");
    }

    [Fact]
    public void ToAgeInYearsString_WithBirthDateOneYearAgoTomorrow_ReturnsZeroYearsOld()
    {
        // Arrange
        var oneYearAgoTomorrow = DateTime.Today.AddYears(-1).AddDays(1);

        // Act
        var result = oneYearAgoTomorrow.ToAgeInYearsString();

        // Assert
        result.ShouldBe("0 years old");
    }

    [Fact]
    public void ToAgeInYearsString_WithBirthDateTenYearsAgo_ReturnsTenYearsOld()
    {
        // Arrange
        var tenYearsAgo = DateTime.Today.AddYears(-10);

        // Act
        var result = tenYearsAgo.ToAgeInYearsString();

        // Assert
        result.ShouldBe("10 years old");
    }

    [Fact]
    public void ToAgeInYearsString_WithBirthDateTenYearsAgoTomorrow_ReturnsNineYearsOld()
    {
        // Arrange
        var tenYearsAgoTomorrow = DateTime.Today.AddYears(-10).AddDays(1);

        // Act
        var result = tenYearsAgoTomorrow.ToAgeInYearsString();

        // Assert
        result.ShouldBe("9 years old");
    }

    [Fact]
    public void ToAgeInYearsString_WithVeryOldBirthDate_ReturnsCorrectAge()
    {
        // Arrange
        var birthDate = new DateTime(1900, 1, 1);

        // Act
        var result = birthDate.ToAgeInYearsString();

        // Assert
        var expectedAge = DateTime.Today.Year - 1900;
        if (new DateTime(DateTime.Today.Year, 1, 1) > DateTime.Today)
        {
            expectedAge--; // Birthday hasn't occurred yet this year
        }
        result.ShouldBe($"{expectedAge} years old");
    }

    [Theory]
    [InlineData(1)]
    [InlineData(5)]
    [InlineData(25)]
    [InlineData(50)]
    [InlineData(100)]
    public void ToAgeInYearsString_WithVariousAges_ReturnsCorrectString(int yearsAgo)
    {
        // Arrange
        var birthDate = DateTime.Today.AddYears(-yearsAgo);

        // Act
        var result = birthDate.ToAgeInYearsString();

        // Assert
        result.ShouldBe($"{yearsAgo} years old");
    }

    [Fact]
    public void ToAgeInYearsString_WithLeapYearBirthdayBeforeLeapDay_ReturnsCorrectAge()
    {
        // Arrange - test with a date before Feb 29 in a leap year scenario
        var birthDate = new DateTime(2000, 2, 28); // 2000 was a leap year

        // Act
        var result = birthDate.ToAgeInYearsString();

        // Assert
        var expectedAge = DateTime.Today.Year - 2000;
        if (new DateTime(DateTime.Today.Year, 2, 28) > DateTime.Today)
        {
            expectedAge--; // Birthday hasn't occurred yet this year
        }
        result.ShouldBe($"{expectedAge} years old");
    }
}
