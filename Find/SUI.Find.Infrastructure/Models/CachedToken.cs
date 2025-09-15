namespace SUI.Find.Infrastructure.Models;

public sealed class CachedToken
{
    public string AccessToken { get; }
    public DateTimeOffset Expiration { get; }

    public CachedToken(string accessToken, int expiresInSeconds)
    {
        AccessToken = accessToken;
        // A buffer of 60 seconds is subtracted to prevent using a token right
        // as it expires, accounting for network latency and clock skew.
        Expiration = DateTimeOffset.UtcNow.AddSeconds(Math.Max(0, expiresInSeconds - 60));
    }

    public bool IsValid() => !string.IsNullOrEmpty(AccessToken) && Expiration > DateTimeOffset.UtcNow;
}