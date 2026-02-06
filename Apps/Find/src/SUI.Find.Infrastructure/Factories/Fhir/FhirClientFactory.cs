using System.Diagnostics.CodeAnalysis;
using System.Net.Http.Headers;
using Hl7.Fhir.Rest;
using Microsoft.Extensions.Options;
using SUI.Find.Infrastructure.Interfaces.Fhir;
using SUI.Find.Infrastructure.Models.Fhir;

namespace SUI.Find.Infrastructure.Factories.Fhir;

[ExcludeFromCodeCoverage(Justification = "Factory class on concrete FhirClient creation")]
public class FhirClientFactory(
    IOptions<AuthTokenServiceConfig> nhsAuthConfig,
    IFhirAuthTokenService fhirAuthTokenService
) : IFhirClientFactory
{
    public async Task<FhirClient> CreateFhirClientAsync(
        CancellationToken cancellationToken = default
    )
    {
        var baseUri =
            nhsAuthConfig.Value.NHS_DIGITAL_FHIR_ENDPOINT
            ?? throw new ArgumentNullException(nhsAuthConfig.Value.NHS_DIGITAL_FHIR_ENDPOINT);

        var fhirClient = new FhirClient(new Uri(baseUri));
        if (fhirClient.RequestHeaders == null)
            return fhirClient;

        var accessToken = await fhirAuthTokenService
            .GetBearerToken(cancellationToken)
            .ConfigureAwait(false);

        fhirClient.RequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer",
            accessToken
        );
        fhirClient.RequestHeaders.Add("X-Request-ID", Guid.NewGuid().ToString());
        return fhirClient;
    }
}
