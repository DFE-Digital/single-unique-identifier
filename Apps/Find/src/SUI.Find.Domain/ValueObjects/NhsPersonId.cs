using SUI.Find.Domain.Models;

namespace SUI.Find.Domain.ValueObjects;

public class NhsPersonId
{
    public string Value { get; init; }

    private NhsPersonId(string value)
    {
        Value = value;
    }

    public static Result<NhsPersonId> Create(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return Result<NhsPersonId>.Fail("NHS Person ID cannot be empty.");

        var cleaned = value.Replace(" ", "");

        if (cleaned.Length != 10)
            return Result<NhsPersonId>.Fail("NHS Person ID must be 10 characters long.");

        if (!cleaned.All(char.IsDigit))
            return Result<NhsPersonId>.Fail("NHS Person ID must contain only digits.");

        if (cleaned[0] == '0')
            return Result<NhsPersonId>.Fail("NHS Person ID cannot start with 0.");

        if (!IsValidNhsNumberChecksum(cleaned))
            return Result<NhsPersonId>.Fail("NHS Person ID has an invalid checksum.");

        return Result<NhsPersonId>.Ok(new NhsPersonId(cleaned));
    }

    private static bool IsValidNhsNumberChecksum(string nhsNumber)
    {
        var digits = nhsNumber.Select(c => c - '0').ToArray();
        var checkDigit = digits[9];

        var sum = 0;
        for (var i = 0; i < 9; i++)
        {
            sum += digits[i] * (10 - i);
        }

        var remainder = sum % 11;
        var expectedCheckDigit = 11 - remainder;

        if (expectedCheckDigit == 11)
            expectedCheckDigit = 0;

        return expectedCheckDigit != 10 && expectedCheckDigit == checkDigit;
    }
}
