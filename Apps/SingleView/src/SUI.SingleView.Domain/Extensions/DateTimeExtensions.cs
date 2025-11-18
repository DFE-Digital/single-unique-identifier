namespace SUI.SingleView.Domain.Extensions;

public static class DateTimeExtensions
{
    private static int ToAgeInYears(this DateTime dateOfBirth)
    {
        var today = DateTime.Today;
        if (dateOfBirth > today)
        {
            return 0;
        }
        var age = today.Year - dateOfBirth.Year;

        // Adjust if birthday hasn't occurred yet this year
        if (dateOfBirth.Date > today.AddYears(-age))
        {
            age--;
        }

        return age;
    }

    public static string ToAgeInYearsString(this DateTime dateOfBirth) =>
        $"{dateOfBirth.ToAgeInYears()} years old";
}
