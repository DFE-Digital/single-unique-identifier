using JetBrains.Annotations;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace SUI.SingleView.Web.UnitTests.Pages;

public abstract class PageModelTestBase<[MeansTestSubject] T>
    where T : PageModel
{
    private protected ILogger<T> MockLogger { get; } = Substitute.For<ILogger<T>>();

    private protected HttpContext MockHttpContext { get; } = Substitute.For<HttpContext>();

    private protected PageContext GetPageContext()
    {
        var modelState = new ModelStateDictionary();
        var actionContext = new ActionContext(
            MockHttpContext,
            new RouteData(),
            new PageActionDescriptor(),
            modelState
        );
        var pageContext = new PageContext(actionContext);
        return pageContext;
    }
}
