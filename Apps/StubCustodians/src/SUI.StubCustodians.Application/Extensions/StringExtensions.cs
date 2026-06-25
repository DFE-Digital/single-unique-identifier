namespace SUI.StubCustodians.Application.Extensions;

public static class StringExtensions
{
    /// <summary>
    /// Ensures that the base URL has a trailing slash to work with RFC 3986.
    /// </summary>
    public static string EnsureTrailingSlash(this string baseUrl) => baseUrl.TrimEnd('/') + '/';
}
