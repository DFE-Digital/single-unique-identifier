namespace SUI.AuthEmulator.Services;

public interface IJwtTokenService
{
    string GenerateToken(string clientId, IReadOnlyList<string> scopes);
}
