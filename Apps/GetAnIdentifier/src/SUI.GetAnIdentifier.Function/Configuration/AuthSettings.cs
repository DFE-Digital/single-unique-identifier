namespace SUI.GetAnIdentifier.Function.Configuration;

public class AuthSettings
{
    public const string SectionName = "AuthSettings";

    public bool UseAuthStoreForAuthorisation { get; set; }

    // OIDC Discovery configuration properties
    public string Issuer { get; set; } =
        "https://sandbox.api.example.gov.uk/sui-find-a-record/auth";
    public string Audience { get; set; } = "sui-find-a-record-api";
    public string OidcDiscoveryUrl { get; set; } =
        "https://localhost:7250/api/v1/.well-known/openid-configuration";

    // The AccessTokenUrl is specifically needed for the OpenAPI spec generation.
    public string AccessTokenUrl { get; set; } = "https://localhost:7250/api/v1/auth/token";
}
