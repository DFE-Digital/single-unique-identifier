using System.Diagnostics;
using Microsoft.AspNetCore.Http;
using Shouldly;
using SUI.SingleView.Web.Pages;

namespace SUI.SingleView.Web.UnitTests.Pages;

public class ErrorPageTests : PageModelTestBase<ErrorModel>
{
    [Fact]
    public void OnGet_SetsShowRequestId()
    {
        Activity.Current = new Activity("TestErrorPageModel").Start();
        var sut = new ErrorModel(MockLogger);
        sut.OnGet();
        Assert.True(sut.ShowRequestId);
        Activity.Current.Stop();
    }

    [Fact]
    public void OnGet_UsesHttpContextTraceIdentifier_WhenActivityNull()
    {
        Activity.Current = null;
        var sut = new ErrorModel(MockLogger)
        {
            PageContext = { HttpContext = new DefaultHttpContext() },
        };

        sut.OnGet();

        sut.ShowRequestId.ShouldBeTrue();
        sut.RequestId.ShouldBe(sut.HttpContext.TraceIdentifier);
    }
}
