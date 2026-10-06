using System.Diagnostics.CodeAnalysis;

namespace SUI.Shared;

/// <summary>
/// Validates NHS numbers: exactly ten ASCII digits, not starting with zero, whose last digit is the
/// Modulus 11 check digit of the first nine. Input is not normalised - spaces, hyphens or other
/// formatting make a value invalid, so callers accepting human-entered values must strip them first.
/// </summary>
public static class NhsNumberValidator
{
    private const int Length = 10;

    public static bool IsValid([NotNullWhen(true)] string? value) =>
        value is { Length: Length }
        && value.All(char.IsAsciiDigit)
        && value[0] != '0'
        && HasValidCheckDigit(value);

    private static bool HasValidCheckDigit(string value)
    {
        var sum = 0;
        for (var i = 0; i < Length - 1; i++)
        {
            sum += (value[i] - '0') * (Length - i);
        }

        var expectedCheckDigit = 11 - (sum % 11);

        // A remainder of 0 gives a check digit of 0; one of 1 gives 10, which no NHS number is
        // issued with.
        if (expectedCheckDigit == 11)
        {
            expectedCheckDigit = 0;
        }

        return expectedCheckDigit != 10 && expectedCheckDigit == value[Length - 1] - '0';
    }
}
