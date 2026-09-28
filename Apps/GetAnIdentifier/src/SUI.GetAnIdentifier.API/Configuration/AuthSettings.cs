using System.ComponentModel.DataAnnotations;

namespace SUI.GetAnIdentifier.API.Configuration;

public class AuthSettings
{
    public const string SectionName = "AuthSettings";

    public bool UseAuthStoreForAuthorisation { get; set; }

    // OIDC Discovery configuration properties
    [Required(ErrorMessage = "OIDC Issuer is required")]
    [AbsoluteHttpsUri(ErrorMessage = "OIDC Issuer must be an absolute HTTPS URI")]
    public required string Issuer { get; set; }

    [Required(ErrorMessage = "OIDC Audience is required")]
    public required string Audience { get; set; }

    [Required(ErrorMessage = "OIDC Discovery URL is required")]
    [Url(ErrorMessage = "OIDC Discovery URL must be a valid URL")]
    public required string OidcDiscoveryUrl { get; set; }

    // The AccessTokenUrl is specifically needed for the OpenAPI spec generation.
    [Required(ErrorMessage = "Access Token URL is required for OpenAPI generation")]
    [Url(ErrorMessage = "Access Token URL must be a valid URL")]
    public required string AccessTokenUrl { get; set; }
}
