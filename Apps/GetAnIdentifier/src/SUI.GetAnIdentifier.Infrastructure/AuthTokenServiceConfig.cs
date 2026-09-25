using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;

namespace SUI.GetAnIdentifier.Infrastructure;

[ExcludeFromCodeCoverage]
public class AuthTokenServiceConfig
{
    public static string SectionName => "NhsAuthConfig";

    [Required(ErrorMessage = "NHS Digital Client ID is required")]
    public required string NHS_DIGITAL_CLIENT_ID { get; init; }

    [Required(ErrorMessage = "NHS Digital Key ID (KID) is required")]
    public required string NHS_DIGITAL_KID { get; init; }

    [Required(ErrorMessage = "NHS Digital Private Key is required")]
    public required string NHS_DIGITAL_PRIVATE_KEY { get; init; }

    [Required(ErrorMessage = "NHS Digital FHIR Endpoint is required")]
    [Url(ErrorMessage = "NHS Digital FHIR Endpoint must be a valid URL")]
    public required string NHS_DIGITAL_FHIR_ENDPOINT { get; init; }

    [Required(ErrorMessage = "NHS Digital Token URL is required")]
    [Url(ErrorMessage = "NHS Digital Token URL must be a valid URL")]
    public required string NHS_DIGITAL_TOKEN_URL { get; init; }

    [Required(ErrorMessage = "NHS Digital Access Token Expiry is required")]
    [Range(1, 60, ErrorMessage = "Token expiry must be between 1 and 60 minutes")]
    public int NHS_DIGITAL_ACCESS_TOKEN_EXPIRES_IN_MINUTES { get; init; }
}
