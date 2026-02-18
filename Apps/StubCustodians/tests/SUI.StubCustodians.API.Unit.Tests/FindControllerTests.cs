using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using SUI.StubCustodians.API.Controllers;
using SUI.StubCustodians.Application.Interfaces;
using SUI.StubCustodians.Application.Models;

namespace SUI.StubCustodians.API.Unit.Tests
{
    public class FindControllerTests
    {
        private readonly FindController _findController;
        private readonly IManifestService _manifestService;

        public FindControllerTests()
        {
            var logger = Substitute.For<ILogger<FindController>>();
            _manifestService = Substitute.For<IManifestService>();
            _findController = new FindController(logger, _manifestService)
            {
                ControllerContext = new ControllerContext
                {
                    HttpContext = new DefaultHttpContext(),
                },
            };
        }

        [Fact]
        public async Task GetManifest_ShouldReturnProblem_WhenCancellationTokenIsCancelled()
        {
            const string orgId = "local-authority-01";
            const string recordType = "childrens-services.details";
            const string personId = "llvwLYMyw4gDCN-FblGIYA";
            var cts = new CancellationTokenSource();
            var ct = cts.Token;
            _manifestService
                .GetManifestForOrganisation(
                    orgId,
                    personId,
                    Arg.Any<string>(),
                    recordType,
                    Arg.Any<CancellationToken>()
                )
                .Throws(new OperationCanceledException());
            await cts.CancelAsync();
            var result = await _findController.GetManifest(orgId, personId, recordType, ct);

            Assert.NotNull(result.Result);
            Assert.IsType<ProblemHttpResult>(result.Result);
            var problemHttpResult = (ProblemHttpResult)result.Result;
            Assert.Equal(499, problemHttpResult.StatusCode);
            Assert.Equal("Client Closed Request", problemHttpResult.ProblemDetails.Title);
            Assert.Equal("Operation Cancelled", problemHttpResult.ProblemDetails.Detail);
        }

        [Fact]
        public async Task GetManifest_ShouldReturnProblem_WhenExceptionIsThrown()
        {
            _findController.ControllerContext.HttpContext.Request.Headers.Add(
                new KeyValuePair<string, StringValues>("X-Forwarded-Host", "localhost")
            );
            _findController.ControllerContext.HttpContext.Request.Headers.Add(
                new KeyValuePair<string, StringValues>("X-Forwarded-Proto", "https")
            );
            const string orgId = "local-authority-01";
            const string recordType = "childrens-services.details";
            const string personId = "llvwLYMyw4gDCN-FblGIYA";
            _manifestService
                .GetManifestForOrganisation(
                    orgId,
                    personId,
                    Arg.Any<string>(),
                    recordType,
                    Arg.Any<CancellationToken>()
                )
                .Throws(new Exception());

            var result = await _findController.PostManifest(
                new FindController.ManifestRequest(orgId, personId, recordType),
                CancellationToken.None
            );

            Assert.NotNull(result.Result);
            Assert.IsType<ProblemHttpResult>(result.Result);
            var problemHttpResult = (ProblemHttpResult)result.Result;
            Assert.Equal(500, problemHttpResult.StatusCode);
            Assert.Equal(
                "An error occurred while processing your request.",
                problemHttpResult.ProblemDetails.Title
            );
            Assert.Equal(
                "Exception of type 'System.Exception' was thrown.",
                problemHttpResult.ProblemDetails.Detail
            );
        }

        [Fact]
        public async Task GetManifest_ShouldReturnManifest_WhenCorrectDetailsAreGiven()
        {
            const string orgId = "local-authority-01";
            const string recordType = "childrens-services.details";
            const string personId = "llvwLYMyw4gDCN-FblGIYA";
            const string recordId = "cd-1234";
            const string recordUrl = $"http://localhost:7256/api/v1/fetch/{orgId}/{recordId}";
            _manifestService
                .GetManifestForOrganisation(
                    orgId,
                    personId,
                    Arg.Any<string>(),
                    recordType,
                    Arg.Any<CancellationToken>()
                )
                .Returns(
                    new List<SearchResultItem> { new(recordType, recordUrl, recordId, orgId) }
                );

            var result = await _findController.GetManifest(
                orgId,
                personId,
                recordType,
                CancellationToken.None
            );

            Assert.NotNull(result.Result);
            Assert.IsType<Ok<IList<SearchResultItem>>>(result.Result);
            var okObjectResult = result.Result as Ok<IList<SearchResultItem>>;
            Assert.NotNull(okObjectResult);
            Assert.Equal(200, okObjectResult.StatusCode);
            Assert.NotNull(okObjectResult.Value);
            Assert.Equal(orgId, okObjectResult.Value.First().SystemId);
            Assert.Equal(recordId, okObjectResult.Value.First().RecordId);
        }
    }
}
