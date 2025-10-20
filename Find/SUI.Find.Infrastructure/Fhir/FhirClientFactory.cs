using System.Net.Http.Headers;
using Hl7.Fhir.Rest;
using SUi.Find.Application.Interfaces;
using SUI.Find.Infrastructure.Interfaces;
using SUI.Find.Infrastructure.Models;

namespace SUI.Find.Infrastructure.Fhir;

public class FhirClientFactory : IFhirClientFactory
{
    private readonly AuthTokenServiceConfig _nhsAuthConfig;
    private readonly IAuthTokenService _authTokenService;

    public FhirClientFactory(IAuthTokenService authTokenService, AuthTokenServiceConfig nhsAuthConfig)
    {
        _nhsAuthConfig = nhsAuthConfig;
        _authTokenService = authTokenService;
    }

    public FhirClient CreateFhirClient()
    {
        var baseUri = _nhsAuthConfig.NHS_DIGITAL_FHIR_ENDPOINT;
        var fhirClient = new FhirClient(new Uri(baseUri));

        if (fhirClient.RequestHeaders != null)
        {
            var accessToken = _authTokenService.GetBearerToken(CancellationToken.None).Result;
            fhirClient.RequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
            fhirClient.RequestHeaders.Add("X-Request-ID", Guid.NewGuid().ToString());
        }

        return fhirClient;
    }
}