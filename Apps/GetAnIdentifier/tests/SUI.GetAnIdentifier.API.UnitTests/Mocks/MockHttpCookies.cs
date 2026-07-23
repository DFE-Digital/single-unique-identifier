using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Primitives;

namespace SUI.GetAnIdentifier.API.UnitTests.Mocks;

public class MockHttpCookies : HttpCookies
{
    private static Dictionary<string, StringValues> Cookies => new();

    public override void Append(string name, string value)
    {
        Cookies[name] = Cookies.TryGetValue(name, out var cookie)
            ? StringValues.Concat(cookie, value)
            : value;
    }

    public override void Append(IHttpCookie cookie)
    {
        Cookies[cookie.Name] = cookie.Value;
    }

    public override IHttpCookie CreateNew() =>
        new HttpCookie(nameof(HttpCookie.Name), nameof(HttpCookie.Value));
}
