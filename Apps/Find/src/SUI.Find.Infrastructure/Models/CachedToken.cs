namespace SUI.Find.Infrastructure.Models;

internal sealed record CachedToken(string AccessToken, DateTimeOffset ExpiresAtUtc);
