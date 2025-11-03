namespace SUI.Find.Infrastructure.Models;

public class AuthTokenServiceConfig
{
    public string? NHS_DIGITAL_TOKEN_URL { get; init; }
    public string? NHS_DIGITAL_CLIENT_ID { get; init; }
    public string? NHS_DIGITAL_KID { get; init; }
    public string? NHS_DIGITAL_PRIVATE_KEY { get; init; }
    public string? NHS_DIGITAL_FHIR_ENDPOINT { get; init; }
    public int? NHS_DIGITAL_ACCESS_TOKEN_EXPIRES_IN_MINUTES { get; init; }
}