namespace SUI.Find.E2ETests;

public record Config
{
    public string BaseUrl
    {
        get;
        init => field = EnsureTrailingSlash(value);
    } = "http://localhost:7182/api/";

    public string StubCustodiansBaseUrl
    {
        get;
        init => field = EnsureTrailingSlash(value);
    } = "https://localhost:7256/api/";

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

    /// <summary>
    /// Ensures that the base URL has a trailing slash to work with RFC 3986.
    /// </summary>
    /// <remarks>
    /// Because of the official web standard (RFC 3986) for resolving URIs, if the base address does not end with a
    /// trailing slash (/), the last segment of the path is treated as a file, not a directory.
    /// This behaviour exists in .NET and hence breaks the configuration if it is missing a trailing slash.
    /// Hence, this method ensures that the base address always ends with a trailing slash.
    /// </remarks>
    private static string EnsureTrailingSlash(string baseUrl) => baseUrl.TrimEnd('/') + '/';
}
