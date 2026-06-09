namespace SUI.AuthEmulator.Configurations;

public class AuthSettings
{
    public const string SectionName = "AuthSettings";

    public string Issuer { get; set; } =
        "https://sandbox.api.example.gov.uk/sui-find-a-record/auth";

    public string BaseUrl { get; set; } = "https://localhost:7250";

    public string Audience { get; set; } = "sui-find-a-record-api";

    public int TokenLifetimeMinutes { get; set; } = 60;
}
