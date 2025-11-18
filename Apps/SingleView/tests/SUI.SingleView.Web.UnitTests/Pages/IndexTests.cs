using Microsoft.AspNetCore.Mvc.RazorPages;
using Shouldly;
using Index = SUI.SingleView.Web.Pages.Index;

namespace SUI.SingleView.Web.UnitTests.Pages;

public class IndexTests : PageModelTestBase<Index>
{
    [Fact]
    public void OnGet_ReturnsPage()
    {
        var sut = new Index(MockLogger);

        // Act
        var result = sut.OnGet();

        // Assert
        result.ShouldBeOfType<PageResult>();
    }
}
