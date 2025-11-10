using System.Net.Http.Headers;
using Hl7.Fhir.Rest;
using Microsoft.Extensions.Options;
using SUI.Matching.Application.Interfaces;
using SUI.Matching.Infrastructure.Interfaces;
using SUI.Matching.Infrastructure.Models;

namespace SUI.Matching.Infrastructure.Fhir;

public class FhirClientFactory(
    IAuthTokenService authTokenService,
    IOptions<AuthTokenServiceConfig> nhsAuthConfig
) : IFhirClientFactory
{
    public FhirClient CreateFhirClient()
    {
        var baseUri = nhsAuthConfig.Value.NHS_DIGITAL_FHIR_ENDPOINT;
        var fhirClient = new FhirClient(new Uri(baseUri ?? string.Empty));

        if (fhirClient.RequestHeaders == null)
            return fhirClient;
        var accessToken = authTokenService.GetBearerToken(CancellationToken.None).Result;
        fhirClient.RequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer",
            accessToken
        );
        fhirClient.RequestHeaders.Add("X-Request-ID", Guid.NewGuid().ToString());

        return fhirClient;
    }
}
