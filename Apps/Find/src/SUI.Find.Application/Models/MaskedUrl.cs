namespace SUI.Find.Application.Models;

public sealed record MaskedUrl(string FetchId, string Url, DateTimeOffset ExpiresAtUtc);
