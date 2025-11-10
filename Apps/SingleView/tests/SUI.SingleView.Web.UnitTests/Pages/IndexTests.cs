using Microsoft.AspNetCore.Mvc.RazorPages;
using Shouldly;
using SUI.SingleView.Web.Pages;

namespace SUI.SingleView.Web.UnitTests.Pages;

public class IndexTests : PageModelTestBase<IndexModel>
{
    [Fact]
    public void OnGet_ReturnsPage()
    {
        var sut = new IndexModel(MockLogger);

        // Act
        var result = sut.OnGet();

        // Assert
        result.ShouldBeOfType<PageResult>();
    }
}