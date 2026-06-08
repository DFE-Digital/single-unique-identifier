namespace SUI.AuthEmulator.Configurations;

public class AuthSettings
{
    public const string SectionName = "AuthSettings";

    public string Issuer { get; set; } =
        "https://sandbox.api.example.gov.uk/sui-find-a-record/auth";

    public string BaseUrl { get; set; } = "https://localhost:7250";
}
