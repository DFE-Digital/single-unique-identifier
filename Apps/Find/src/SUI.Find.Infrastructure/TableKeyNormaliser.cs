namespace SUI.Find.Infrastructure;

public static class TableKeyNormaliser
{
    public static string Normalise(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Key value cannot be null or empty.");
        }

        // 1.Trim
        value = value.Trim();

        // 2.Uppercase for consistency
        value = value.ToUpperInvariant();

        // 3.Replace forbidden characters
        return value.Replace("/", "_").Replace("\\", "_").Replace("#", "_").Replace("?", "_");
    }
}
