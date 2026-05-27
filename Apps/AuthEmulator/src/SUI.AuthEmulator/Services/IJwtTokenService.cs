namespace SUI.AuthEmulator.Services;

public interface IJwtTokenService
{
    Task<string> GenerateToken(string clientId, IReadOnlyList<string> scopes);
}
