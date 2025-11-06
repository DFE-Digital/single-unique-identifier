namespace SUI.Find.Infrastructure.Models;

public sealed class CachedToken(string accessToken, int expiresInSeconds)
{
    public string AccessToken { get; } = accessToken;

    // A buffer of 60 seconds is subtracted to prevent using a token right
    // as it expires, accounting for network latency and clock skew.
    private DateTimeOffset Expiration { get; } = DateTimeOffset.UtcNow.AddSeconds(Math.Max(0, expiresInSeconds - 60));

    public bool IsValid() => !string.IsNullOrEmpty(AccessToken) && Expiration > DateTimeOffset.UtcNow;
}