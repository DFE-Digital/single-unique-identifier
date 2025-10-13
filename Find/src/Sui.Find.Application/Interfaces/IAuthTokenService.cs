namespace SUI.Find.Application.Interfaces;

public interface IAuthTokenService
{
    Task<string> GetBearerToken(CancellationToken cancellationToken);
}