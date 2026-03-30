using Microsoft.AspNetCore.Http;
using SUI.StubCustodians.Application.Interfaces;

namespace SUI.StubCustodians.Application.Services;

public class HttpContextBaseUrlProvider : IBaseUrlProvider
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public HttpContextBaseUrlProvider(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public string GetBaseUrl()
    {
        var ctx = _httpContextAccessor.HttpContext;

        if (ctx == null)
        {
            throw new InvalidOperationException("No HttpContext available");
        }

        var req = ctx.Request;

        var host = req.Headers.TryGetValue("X-Forwarded-Host", out var fwdHost)
            ? fwdHost.FirstOrDefault()
            : req.Host.Host;

        var proto = req.Headers.TryGetValue("X-Forwarded-Proto", out var fwdProto)
            ? fwdProto.FirstOrDefault()
            : req.Scheme;

        var port = req.Host.Port;

        var includePort =
            port.HasValue && !(proto == "http" && port == 80) && !(proto == "https" && port == 443);

        return includePort ? $"{proto}://{host}:{port}" : $"{proto}://{host}";
    }
}
