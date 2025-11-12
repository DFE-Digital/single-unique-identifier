using System.Diagnostics;
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
}
