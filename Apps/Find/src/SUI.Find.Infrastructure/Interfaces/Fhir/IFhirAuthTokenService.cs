namespace SUI.Find.Infrastructure.Interfaces.Fhir;

public interface IFhirAuthTokenService
{
    Task<string> GetBearerToken(CancellationToken cancellationToken);
}
