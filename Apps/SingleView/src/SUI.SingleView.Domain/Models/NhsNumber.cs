namespace SUI.SingleView.Domain.Models;

public readonly record struct NhsNumber
{
    public string Value { get; }

    private NhsNumber(string value)
    {
        Value = value;
    }

    public static bool TryParse(string? input, out NhsNumber result)
    {
        result = default;

        if (string.IsNullOrWhiteSpace(input))
            return false;

        var digits = new string(input.Where(char.IsDigit).ToArray());
        if (digits.Length != 10)
            return false;

        if (!IsValidChecksum(digits))
            return false;

        result = new NhsNumber(digits);
        return true;
    }

    public static NhsNumber Parse(string input) =>
        TryParse(input, out var parsed) ? parsed : throw new FormatException("Invalid NHS number.");

    public static bool IsValid(string? input) => TryParse(input, out _);

    public override string ToString() =>
        $"{Value[..3]} {Value.Substring(3, 3)} {Value.Substring(6, 4)}";

    internal static bool IsValidChecksum(string digits)
    {
        var sum = 0;
        for (var i = 0; i < 9; i++)
        {
            var digit = digits[i] - '0';
            sum += digit * (10 - i);
        }

        var remainder = sum % 11;
        var checkDigit = 11 - remainder;
        if (checkDigit == 11)
            checkDigit = 0;
        if (checkDigit == 10)
            return false;

        return checkDigit == (digits[9] - '0');
    }

    public static NhsNumber FromDigits(string digits) => new(digits);

    public static implicit operator string(NhsNumber nhs) => nhs.Value;

    public static implicit operator NhsNumber(string input) => Parse(input);
}
