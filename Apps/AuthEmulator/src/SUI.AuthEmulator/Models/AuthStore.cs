namespace SUI.AuthEmulator.Models;

public class AuthStore
{
    public string Issuer { get; init; } = string.Empty;
    public string Audience { get; init; } = string.Empty;
    public string SigningKey { get; init; } = string.Empty;
    public required int DefaultTokenLifetimeMinutes { get; init; } = 60;
    public List<AuthClient>? Clients { get; set; }
}
