namespace SUI.Find.Infrastructure.Interfaces.Fhir;

public interface IAuthTokenService
{
    Task<string> GetBearerToken(CancellationToken cancellationToken);
}
