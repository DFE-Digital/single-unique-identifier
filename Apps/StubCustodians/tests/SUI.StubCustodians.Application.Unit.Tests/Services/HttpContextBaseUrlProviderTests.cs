using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;
using NSubstitute;
using SUI.StubCustodians.Application.Services;

namespace SUI.StubCustodians.Application.Unit.Tests.Services;

public class HttpContextBaseUrlProviderTests
{
    [Fact]
    public void GetBaseUrl_ShouldThrow_WhenHttpContextMissing()
    {
        var accessor = Substitute.For<IHttpContextAccessor>();
        accessor.HttpContext.Returns((HttpContext?)null);

        var provider = new HttpContextBaseUrlProvider(accessor);

        Assert.Throws<InvalidOperationException>(() => provider.GetBaseUrl());
    }

    [Fact]
    public void GetBaseUrl_ShouldReturnSchemeHostAndPort_WhenNonDefaultPort()
    {
        var context = new DefaultHttpContext();

        context.Request.Scheme = "https";
        context.Request.Host = new HostString("localhost", 5001);

        var accessor = Substitute.For<IHttpContextAccessor>();
        accessor.HttpContext.Returns(context);

        var provider = new HttpContextBaseUrlProvider(accessor);

        var result = provider.GetBaseUrl();

        Assert.Equal("https://localhost:5001", result);
    }

    [Fact]
    public void GetBaseUrl_ShouldOmitPort_WhenDefaultHttpsPort()
    {
        var context = new DefaultHttpContext();

        context.Request.Scheme = "https";
        context.Request.Host = new HostString("example.com", 443);

        var accessor = Substitute.For<IHttpContextAccessor>();
        accessor.HttpContext.Returns(context);

        var provider = new HttpContextBaseUrlProvider(accessor);

        var result = provider.GetBaseUrl();

        Assert.Equal("https://example.com", result);
    }

    [Fact]
    public void GetBaseUrl_ShouldUseForwardedHeaders_WhenPresent()
    {
        var context = new DefaultHttpContext();

        context.Request.Scheme = "http";
        context.Request.Host = new HostString("internal-host", 8080);

        context.Request.Headers["X-Forwarded-Host"] = new StringValues("public.example.com");
        context.Request.Headers["X-Forwarded-Proto"] = new StringValues("https");

        var accessor = Substitute.For<IHttpContextAccessor>();
        accessor.HttpContext.Returns(context);

        var provider = new HttpContextBaseUrlProvider(accessor);

        var result = provider.GetBaseUrl();

        Assert.Equal("https://public.example.com:8080", result);
    }
}
