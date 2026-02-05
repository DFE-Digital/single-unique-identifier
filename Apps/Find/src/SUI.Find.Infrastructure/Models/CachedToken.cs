namespace SUI.Find.Infrastructure.Models;

internal sealed record CachedToken(string AccessToken, DateTimeOffset ExpiresAtUtc)
{
    public bool IsValid() =>
        !string.IsNullOrEmpty(AccessToken) && ExpiresAtUtc > DateTimeOffset.UtcNow;
}
