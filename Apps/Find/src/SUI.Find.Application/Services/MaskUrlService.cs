namespace SUI.Find.Application.Services;

public class MaskUrlService()
{
    public string Create(string originalUrl)
    {
        // TODO: Log the guid and correlation Id's so we can trace back if needed
        var newId = Guid.NewGuid().ToString("N");
        // Simple masking logic for demonstration purposes
        var maskedUrl = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(originalUrl));
        return $"https://masked.url/{maskedUrl}";
    }
}
