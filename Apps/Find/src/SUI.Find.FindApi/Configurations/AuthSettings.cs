namespace SUI.Find.FindApi.Configurations;

public class AuthSettings
{
    public const string SectionName = "AuthSettings";

    // OIDC Discovery configuration properties
    public string Issuer { get; set; } =
        "https://sandbox.api.example.gov.uk/sui-find-a-record/auth";
    public string Audience { get; set; } = "sui-find-a-record-api";
    public string OidcDiscoveryUrl { get; set; } =
        "https://localhost:7250/api/v1/.well-known/openid-configuration";
}
