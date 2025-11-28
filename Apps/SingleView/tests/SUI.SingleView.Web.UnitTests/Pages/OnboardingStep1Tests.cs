using Microsoft.AspNetCore.Mvc.RazorPages;
using Shouldly;
using SUI.SingleView.Web.Pages;

namespace SUI.SingleView.Web.UnitTests.Pages;

public class OnboardingStep1Tests : PageModelTestBase<OnboardingStep1>
{
    [Fact]
    public void OnGet_ReturnsPage()
    {
        // Arrange
        var sut = new OnboardingStep1(MockLogger);

        // Act
        var result = sut.OnGet();

        // Assert
        result.ShouldBeOfType<PageResult>();
    }
}
