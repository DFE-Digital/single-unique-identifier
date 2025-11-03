namespace SUi.Find.Application.Interfaces;

public interface IAuthTokenService
{
    Task<string> GetBearerToken(CancellationToken cancellationToken);
}