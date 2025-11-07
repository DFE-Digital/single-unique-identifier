namespace SUI.Matching.Application.Interfaces;

public interface IAuthTokenService
{
    Task<string> GetBearerToken(CancellationToken cancellationToken);
}