namespace SUI.NotificationService.Infrastructure.Utilities;

public static class TableKeyNormaliser
{
    public static string Normalise(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Key value cannot be null or empty.");
        }

        // Trim
        value = value.Trim();

        // Uppercase for consistency
        value = value.ToUpperInvariant();

        // Replace forbidden characters
        return value.Replace("/", "_").Replace("\\", "_").Replace("#", "_").Replace("?", "_");
    }
}
