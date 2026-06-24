namespace SUI.Find.E2ETests;

public record Config
{
    public string BaseUrl { get; init; } = "http://localhost:7182/api/";

    public string StubCustodiansBaseUrl { get; init; } = "https://localhost:7256/api/";

    public bool IsLocal => BaseUrl.Contains("localhost");

    public bool SkipResetAzureTables { get; init; }

    public string? FindApiStorageConnectionString { get; init; } = "UseDevelopmentStorage=true";

    public string? FindApiKey { get; init; }

    public string? PreviousFindApiKey { get; init; }

    public DateTimeOffset? CheckFindApiBuildTimestampThreshold { get; init; }

    public DateTimeOffset? CheckStubCustodiansApiBuildTimestampThreshold { get; init; }

    public string AccessTokenUrl { get; init; } = "https://localhost:7250/api/v1/auth/token";

    /// <summary>
    /// Optional ability to override the scope to use when requesting access tokens, to support running the tests through FaUAPI or other gateways.
    /// </summary>
    public string? AuthScope
    {
        get;
        init
        {
            field = value;
            AuthScopes = value != null ? [value] : null;
        }
    }

    public string?[]? AuthScopes { get; private init; }

    public string? AuthEmulatorHealthCheckEndpoint { get; init; } =
        "https://localhost:7250/api/health";

    public DateTimeOffset? CheckAuthEmulatorApiBuildTimestampThreshold { get; init; }
}
