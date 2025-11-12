using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Shouldly;
using SUI.SingleView.Web.Pages;

namespace SUI.SingleView.Web.UnitTests.Pages;

public class OnboardingStep2Tests : PageModelTestBase<OnboardingStep2>
{
    [Fact]
    public async Task OnPost_WhenSelectedOptionsNotPresent_ShowsError()
    {
        var sut = new OnboardingStep2(MockLogger) { PageContext = GetPageContext() };
        sut.OnGet();
        var result = await sut.OnPostAsync();
        result.ShouldBeOfType<PageResult>();
        sut.ShowError.ShouldBeTrue();
    }

    [Fact]
    public async Task OnPost_WhenSelectedOptionsPresent_RedirectsToSigninPage()
    {
        var sut = new OnboardingStep2(MockLogger)
        {
            PageContext = GetPageContext(),
            SelectedOptions = ["terms", "analytics"],
        };
        var result = await sut.OnPostAsync();
        result.ShouldBeOfType<RedirectToPageResult>();
        ((RedirectToPageResult)result).PageName.ShouldBe("./Signin");
    }
}
