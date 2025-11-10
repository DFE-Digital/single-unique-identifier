using System.Diagnostics;
using Microsoft.Extensions.Logging;
using NSubstitute;
using SUI.SingleView.Web.Pages;

namespace SUI.SingleView.Web.UnitTests.Pages;

public class PageModelTests
{
    [Fact]
    public void TestErrorPageModel()
    {
        Activity.Current = new Activity("TestErrorPageModel").Start();
        var logger = Substitute.For<ILogger<ErrorModel>>();
        var sut = new ErrorModel(logger);
        sut.OnGet();
        Assert.True(sut.ShowRequestId);
        Activity.Current.Stop();
    }

    [Fact]
    public void TestHomePageModel()
    {
        var logger = Substitute.For<ILogger<IndexModel>>();
        var sut = new IndexModel(logger);
        sut.OnGet();
        Assert.True(true);
    }
}
