using Microsoft.AspNetCore.Mvc;
using SUI.StubCustodians.Application.Common;

namespace SUI.StubCustodians.API.Unit.Tests
{
    public class HandlerResultExtensionsTests
    {
        [Fact]
        public void ToActionResult_ShouldReturnOk_WhenSuccess()
        {
            var result = new { Name = "Test" };
            var handlerResult = HandlerResult<object>.Success(result);

            var actionResult = handlerResult.ToActionResult();

            var okResult = Assert.IsType<OkObjectResult>(actionResult);
            Assert.Equal(result, okResult.Value);
        }

        [Fact]
        public void ToActionResult_ShouldReturnNotFound_WhenFailureIsNotFound()
        {
            var handlerResult = HandlerResult<object>.NotFound("Item not found");

            var actionResult = handlerResult.ToActionResult();

            Assert.IsType<NotFoundResult>(actionResult);
        }

        [Fact]
        public void ToActionResult_ShouldReturnConflict_WhenFailureIsDataConcurrency()
        {
            var handlerResult = HandlerResult<object>.DataConcurrencyError("Concurrency error");

            var actionResult = handlerResult.ToActionResult();

            var conflictResult = Assert.IsType<ConflictObjectResult>(actionResult);
            var failureInfo = Assert.IsType<FailureInfo>(conflictResult.Value);
            Assert.Equal(FailureKind.DataConcurrency, failureInfo.Kind);
            Assert.Single(failureInfo.Errors);
            Assert.Equal("Concurrency error", failureInfo.Errors.First().Message);
        }

        [Fact]
        public void ToActionResult_ShouldReturnBadRequest_WhenFailureIsValidation()
        {
            var error = new ErrorInfo("Scope1", "Validation failed");
            var handlerResult = HandlerResult<object>.ValidationFailure(error);

            var actionResult = handlerResult.ToActionResult();

            var badRequest = Assert.IsType<BadRequestObjectResult>(actionResult);
            var problemDetails = Assert.IsType<ValidationProblemDetails>(badRequest.Value);
            Assert.Equal("One or more validation errors occurred.", problemDetails.Title);
            Assert.True(problemDetails.Errors.ContainsKey("Scope1"));
            Assert.Contains("Validation failed", problemDetails.Errors["Scope1"]);
            Assert.Equal((int)FailureKind.Validation, problemDetails.Status);
        }

        [Fact]
        public void ToActionResult_ShouldReturnGenericError_WhenFailureHasNoErrors()
        {
            var handlerResult = HandlerResult<object>.ValidationFailure(Array.Empty<ErrorInfo>());

            var actionResult = handlerResult.ToActionResult();

            var badRequest = Assert.IsType<BadRequestObjectResult>(actionResult);
            var problemDetails = Assert.IsType<ValidationProblemDetails>(badRequest.Value);
            Assert.True(problemDetails.Errors.ContainsKey(string.Empty));
            Assert.Contains("Oops something went wrong", problemDetails.Errors[string.Empty]);
        }
    }
}
