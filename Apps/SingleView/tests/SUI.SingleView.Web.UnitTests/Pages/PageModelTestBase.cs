using JetBrains.Annotations;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace SUI.SingleView.Web.UnitTests.Pages;

public abstract class PageModelTestBase<[MeansTestSubject] T> where T : PageModel
{
    private protected ILogger<T> MockLogger { get; } = Substitute.For<ILogger<T>>();
}