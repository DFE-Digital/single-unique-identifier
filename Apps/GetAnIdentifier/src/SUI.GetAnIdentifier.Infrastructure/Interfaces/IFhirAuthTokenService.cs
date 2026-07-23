namespace SUI.GetAnIdentifier.Infrastructure.Interfaces;

public interface IFhirAuthTokenService
{
    Task<string> GetBearerToken(CancellationToken cancellationToken);
}
