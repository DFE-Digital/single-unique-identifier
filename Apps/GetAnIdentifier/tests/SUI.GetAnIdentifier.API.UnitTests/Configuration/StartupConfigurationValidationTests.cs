using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using SUI.GetAnIdentifier.API.Configuration;
using SUI.GetAnIdentifier.Infrastructure;

namespace SUI.GetAnIdentifier.API.UnitTests.Configuration;

public class StartupConfigurationValidationTests
{
    private static ServiceProvider BuildServiceProviderWithConfig(
        Dictionary<string, string?> inMemorySettings
    )
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(inMemorySettings)
            .Build();

        var services = new ServiceCollection();

        services
            .AddOptions<AuthSettings>()
            .Bind(configuration.GetSection(AuthSettings.SectionName))
            .ValidateDataAnnotations();

        services
            .AddOptions<AuthTokenServiceConfig>()
            .Bind(configuration.GetSection(AuthTokenServiceConfig.SectionName))
            .ValidateDataAnnotations();

        return services.BuildServiceProvider();
    }

    [Fact]
    public void AuthSettings_ThrowsValidationException_WhenMissingRequiredValues()
    {
        // Arrange - Missing OIDC Issuer & Audience
        var settings = new Dictionary<string, string?>
        {
            { $"{AuthSettings.SectionName}:OidcDiscoveryUrl", "https://valid.com/.well-known" },
            { $"{AuthSettings.SectionName}:AccessTokenUrl", "https://valid.com/token" },
        };

        var provider = BuildServiceProviderWithConfig(settings);

        // Act & Assert
        var exception = Assert.Throws<OptionsValidationException>(() =>
            provider.GetRequiredService<IOptions<AuthSettings>>().Value
        );

        Assert.Contains("OIDC Issuer is required", exception.Message);
        Assert.Contains("OIDC Audience is required", exception.Message);
    }

    [Fact]
    public void AuthSettings_ThrowsValidationException_WhenUrlIsMalformed()
    {
        // Arrange - Malformed OIDC URL
        var settings = new Dictionary<string, string?>
        {
            { $"{AuthSettings.SectionName}:Issuer", "test-issuer" },
            { $"{AuthSettings.SectionName}:Audience", "test-audience" },
            { $"{AuthSettings.SectionName}:OidcDiscoveryUrl", "not-a-valid-url" }, // INVALID
            { $"{AuthSettings.SectionName}:AccessTokenUrl", "https://valid.com/token" },
        };

        var provider = BuildServiceProviderWithConfig(settings);

        // Act & Assert
        var exception = Assert.Throws<OptionsValidationException>(() =>
            provider.GetRequiredService<IOptions<AuthSettings>>().Value
        );

        Assert.Contains("OIDC Discovery URL must be a valid URL", exception.Message);
    }

    [Fact]
    public void AuthTokenServiceConfig_ThrowsValidationException_WhenMissingNhsSecrets()
    {
        // Arrange - Missing Private Key and KID
        var settings = new Dictionary<string, string?>
        {
            { $"{AuthTokenServiceConfig.SectionName}:NHS_DIGITAL_CLIENT_ID", "client-123" },
            {
                $"{AuthTokenServiceConfig.SectionName}:NHS_DIGITAL_FHIR_ENDPOINT",
                "https://valid.com/fhir"
            },
            {
                $"{AuthTokenServiceConfig.SectionName}:NHS_DIGITAL_TOKEN_URL",
                "https://valid.com/token"
            },
            {
                $"{AuthTokenServiceConfig.SectionName}:NHS_DIGITAL_ACCESS_TOKEN_EXPIRES_IN_MINUTES",
                "5"
            },
        };

        var provider = BuildServiceProviderWithConfig(settings);

        // Act & Assert
        var exception = Assert.Throws<OptionsValidationException>(() =>
            provider.GetRequiredService<IOptions<AuthTokenServiceConfig>>().Value
        );

        Assert.Contains("NHS Digital Private Key is required", exception.Message);
        Assert.Contains("NHS Digital Key ID (KID) is required", exception.Message);
    }

    [Fact]
    public void AuthTokenServiceConfig_ThrowsValidationException_WhenUrlsAreMalformed()
    {
        // Arrange
        var settings = new Dictionary<string, string?>
        {
            { $"{AuthTokenServiceConfig.SectionName}:NHS_DIGITAL_CLIENT_ID", "client-123" },
            { $"{AuthTokenServiceConfig.SectionName}:NHS_DIGITAL_KID", "kid-123" },
            { $"{AuthTokenServiceConfig.SectionName}:NHS_DIGITAL_PRIVATE_KEY", "secret" },
            { $"{AuthTokenServiceConfig.SectionName}:NHS_DIGITAL_FHIR_ENDPOINT", "not-a-url" }, // INVALID
            { $"{AuthTokenServiceConfig.SectionName}:NHS_DIGITAL_TOKEN_URL", "also-not-a-url" }, // INVALID
            {
                $"{AuthTokenServiceConfig.SectionName}:NHS_DIGITAL_ACCESS_TOKEN_EXPIRES_IN_MINUTES",
                "5"
            },
        };

        var provider = BuildServiceProviderWithConfig(settings);

        // Act & Assert
        var exception = Assert.Throws<OptionsValidationException>(() =>
            provider.GetRequiredService<IOptions<AuthTokenServiceConfig>>().Value
        );

        Assert.Contains("NHS Digital FHIR Endpoint must be a valid URL", exception.Message);
        Assert.Contains("NHS Digital Token URL must be a valid URL", exception.Message);
    }

    [Fact]
    public void Configurations_ResolveSuccessfully_WhenAllValid()
    {
        // Arrange
        var settings = new Dictionary<string, string?>
        {
            // AuthSettings
            { $"{AuthSettings.SectionName}:Issuer", "https://valid.com/issuer" },
            { $"{AuthSettings.SectionName}:Audience", "audience" },
            { $"{AuthSettings.SectionName}:OidcDiscoveryUrl", "https://valid.com/.well-known" },
            { $"{AuthSettings.SectionName}:AccessTokenUrl", "https://valid.com/token" },
            // AuthTokenServiceConfig
            { $"{AuthTokenServiceConfig.SectionName}:NHS_DIGITAL_CLIENT_ID", "client-123" },
            { $"{AuthTokenServiceConfig.SectionName}:NHS_DIGITAL_KID", "kid-123" },
            { $"{AuthTokenServiceConfig.SectionName}:NHS_DIGITAL_PRIVATE_KEY", "secret" },
            {
                $"{AuthTokenServiceConfig.SectionName}:NHS_DIGITAL_FHIR_ENDPOINT",
                "https://valid.com/fhir"
            },
            {
                $"{AuthTokenServiceConfig.SectionName}:NHS_DIGITAL_TOKEN_URL",
                "https://valid.com/token"
            },
            {
                $"{AuthTokenServiceConfig.SectionName}:NHS_DIGITAL_ACCESS_TOKEN_EXPIRES_IN_MINUTES",
                "5"
            },
        };

        var provider = BuildServiceProviderWithConfig(settings);

        // Act
        var authSettings = provider.GetRequiredService<IOptions<AuthSettings>>().Value;
        var nhsConfig = provider.GetRequiredService<IOptions<AuthTokenServiceConfig>>().Value;

        // Assert - No exceptions thrown and validation passed
        Assert.NotNull(authSettings);
        Assert.NotNull(nhsConfig);
    }

    [Theory]
    [InlineData("not-a-url")]
    [InlineData("http://insecure.com/auth")] // Fails HTTPS check
    [InlineData("/relative/path")] // Fails Absolute URI check
    public void AuthSettings_ThrowsValidationException_WhenIssuerIsNotAbsoluteHttpsUri(
        string invalidIssuer
    )
    {
        // Arrange
        var settings = new Dictionary<string, string?>
        {
            { $"{AuthSettings.SectionName}:Issuer", invalidIssuer },
            { $"{AuthSettings.SectionName}:Audience", "audience" },
            { $"{AuthSettings.SectionName}:OidcDiscoveryUrl", "https://valid.com/.well-known" },
            { $"{AuthSettings.SectionName}:AccessTokenUrl", "https://valid.com/token" },
        };

        var provider = BuildServiceProviderWithConfig(settings);

        // Act & Assert
        var exception = Assert.Throws<OptionsValidationException>(() =>
            provider.GetRequiredService<IOptions<AuthSettings>>().Value
        );

        Assert.Contains("OIDC Issuer must be an absolute HTTPS URI", exception.Message);
    }
}
