using Bogus;
using SUI.SingleView.Domain.Models;

namespace SUI.SingleView.Domain.UnitTests.Extensions;

public static class BogusExtensions
{
    public static NhsNumber GenerateNhsNumber(this Faker faker, Random? random = null)
    {
        while (true)
        {
            var rng = random ?? Random.Shared;

            var digits = new int[10];

            // Generate first 9 digits
            for (var i = 0; i < 9; i++)
                digits[i] = rng.Next(0, 10);

            var sum = 0;
            for (var i = 0; i < 9; i++)
                sum += digits[i] * (10 - i);

            var remainder = sum % 11;
            var checkDigit = 11 - remainder;
            if (checkDigit == 11)
                checkDigit = 0;

            // 10 is invalid, regenerate
            if (checkDigit == 10)
                continue;

            digits[9] = checkDigit;

            var digitsString = string.Concat(digits.Select(d => (char)('0' + d)));
            return NhsNumber.FromDigits(digitsString);
        }
    }
}
