namespace SUI.Find.Infrastructure.Models;

public sealed class AuthStore
{
    public string Issuer { get; set; } = string.Empty;
    public string Audience { get; set; } = string.Empty;
    public string SigningKey { get; set; } = string.Empty;
    public int DefaultTokenLifetimeMinutes { get; set; } = 60;
    public List<AuthClient>? Clients { get; set; }
}
