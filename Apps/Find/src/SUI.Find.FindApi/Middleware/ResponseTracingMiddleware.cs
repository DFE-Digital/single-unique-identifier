using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Azure.Functions.Worker.Middleware;

namespace SUI.Find.FindApi.Middleware;

[ExcludeFromCodeCoverage(
    Justification = "Waiting on Integration tests to cover middleware functionality."
)]
// ReSharper disable once ClassNeverInstantiated.Global
public class ResponseTracingMiddleware : IFunctionsWorkerMiddleware
{
    public const string TraceIdHeaderName = "Trace-Id";
    public const string InvocationIdHeaderName = "Invocation-Id";

    public async Task Invoke(FunctionContext context, FunctionExecutionDelegate next)
    {
        await next(context);

        var result = context.GetInvocationResult();

        if (result?.Value is HttpResponseData response)
        {
            response.Headers.Add(TraceIdHeaderName, Activity.Current?.TraceId.ToString());
            response.Headers.Add(InvocationIdHeaderName, context.InvocationId);
        }
    }
}
