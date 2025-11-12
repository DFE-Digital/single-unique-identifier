using NSubstitute;
using SUI.SingleView.Web.Pages;

namespace SUI.SingleView.Web.UnitTests.Pages;

public class OnboardingStep1Tests : PageModelTestBase<OnboardingStep1>
{
    [Fact]
    public void OnGet_WhenAcceptedCookiePresent_ReturnsSigninLink()
    {
        MockHttpContext.Request.Cookies.ContainsKey("AcceptedTerms").ReturnsForAnyArgs(true);
        var sut = new OnboardingStep1(MockLogger) { PageContext = GetPageContext() };
        sut.OnGet();
        Assert.Equal("SignIn", sut.NextStep);
    }
}
