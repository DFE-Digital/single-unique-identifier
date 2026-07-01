namespace SUI.UIHarness.Web;

public static class ConfigKeys
{
    /// <summary>
    /// Configuration key for the Base URL of the Find API.
    /// Required.
    /// </summary>
    public const string FindApiBaseUrl = "BaseUrl";

    /// <summary>
    /// Configuration key for the Auth Scope to use when calling the Find API via a Gateway.
    /// Optional, only needed when the Find API is behind a Gateway.
    /// </summary>
    public const string FindApiGatewayAuthScope = "AuthSettings:FindApiGatewayAuthScope";

    /// <summary>
    /// Configuration key for the URL to use to get client credential access tokens for subsequently calling the Find API.
    /// Required.
    /// </summary>
    public const string AccessTokenUrl = "AuthSettings:AccessTokenUrl";

    /// <summary>
    /// Configuration key for the API Key to use when calling the Match endpoint.
    /// Required.
    /// </summary>
    public const string MatchApiKey = "MATCH_API_KEY";
}
