using Microsoft.AspNetCore.Mvc;
using SUI.FakeCustodians.Application.Common;

namespace SUI.FakeCustodians.API
{
    public static class HandlerResultExtensions
    {
        public static IActionResult ToActionResult<T>(this HandlerResult<T> handlerResult) where T : class
        {
            if (handlerResult.IsSuccess)
            {
                return new OkObjectResult(handlerResult.Result);
            }

            if (handlerResult.Failure.Kind == FailureKind.NotFound)
            {
                return new NotFoundResult();
            }

            if (handlerResult.Failure.Kind == FailureKind.DataConcurrency)
            {
                return new ConflictObjectResult(handlerResult.Failure);
            }

            return new BadRequestObjectResult(FailureToProblemDetails(handlerResult.Failure));
        }

        private static ValidationProblemDetails FailureToProblemDetails(FailureInfo failureInfo)
        {
            IDictionary<string, string[]>? validationErrors = null;

            if (failureInfo.Errors.Count != 0)
            {
                var groupedErrors = failureInfo.Errors.GroupBy(e => e.Scope ?? string.Empty);

                validationErrors = groupedErrors.ToDictionary(s => s.Key, s => s.Select(e => e.Message).ToArray());
            }

            if (validationErrors == null)
            {
                validationErrors = new Dictionary<string, string[]> { { string.Empty, ["Oops something went wrong"] } };
            }

            return new ValidationProblemDetails(validationErrors)
            {
                Title = "One or more validation errors occurred.",
                Status = (int)failureInfo.Kind
            };
        }
    }
}