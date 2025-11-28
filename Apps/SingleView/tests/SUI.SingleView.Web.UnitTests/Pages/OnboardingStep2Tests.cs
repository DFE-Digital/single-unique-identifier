using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Shouldly;
using SUI.SingleView.Web.Pages;

namespace SUI.SingleView.Web.UnitTests.Pages;

public class OnboardingStep2Tests : PageModelTestBase<OnboardingStep2>
{
    [Fact]
    public void OnGet_ReturnsPage()
    {
        // Arrange
        var sut = new OnboardingStep2(MockLogger);

        // Act
        var result = sut.OnGet();

        // Assert
        result.ShouldBeOfType<PageResult>();
    }
}
