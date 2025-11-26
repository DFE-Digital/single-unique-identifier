using System.Diagnostics.CodeAnalysis;

namespace SUI.Find.FindApi.Models.Auth;

[ExcludeFromCodeCoverage(Justification = "Auth models do not contain any logic to be tested.")]
public class AuthTokenFormRequest
{
    public string client_id { get; set; } = string.Empty;
    public string client_secret { get; set; } = string.Empty;
    public string scope { get; set; } = string.Empty;
    public string grant_type { get; set; } = "client_credentials";
}
