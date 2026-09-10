using System.Net;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Azure.Functions.Worker.Middleware;
using Microsoft.Extensions.Logging;
using SUI.GetAnIdentifier.API.Utility;
using SUI.GetAnIdentifier.Infrastructure.Interfaces;
using AuditEvent = SUI.GetAnIdentifier.Infrastructure.Models.AuditEvent;
using Task = System.Threading.Tasks.Task;

namespace SUI.GetAnIdentifier.API.Middleware;

public class AuditMiddleware(
    ILogger<AuditMiddleware> logger,
    IAuditService auditService,
    TimeProvider timeProvider
) : IFunctionsWorkerMiddleware
{
    private const string SwaggerPathSegment = "swagger";
    private const string TokenPathSegment = "auth/token";

    public async Task Invoke(FunctionContext context, FunctionExecutionDelegate next)
    {
        var correlationId = context.InvocationId;

        logger.BeginScope(new Dictionary<string, object> { ["CorrelationId"] = correlationId });

        var request = await context.GetHttpRequestDataAsync();
        if (request is null)
        {
            logger.LogError("Endpoint can only process HTTP requests.");
            return;
        }

        try
        {
            await AuditIncomingRequest(context, correlationId, request);
        }
        catch (Exception)
        {
            context.GetInvocationResult().Value = await HttpResponseUtility.ProblemResponse(
                request,
                HttpStatusCode.InternalServerError,
                "Audit error",
                $"An error occurred while attempting to audit incoming request. CorrelationId: {correlationId}"
            );
            return;
        }

        await next(context);

        try
        {
            await AuditOutgoingResponse(context, correlationId);
        }
        catch (Exception)
        {
            context.GetInvocationResult().Value = await HttpResponseUtility.ProblemResponse(
                request,
                HttpStatusCode.InternalServerError,
                "Audit error",
                $"An error occurred while attempting to audit outgoing response.  CorrelationId: {correlationId}"
            );
        }
    }

    private async Task AuditIncomingRequest(
        FunctionContext context,
        string correlationId,
        HttpRequestData request
    )
    {
        // No requirement to audit swagger or token requests
        var isSwaggerRequest = request.Url.AbsolutePath.Contains(
            SwaggerPathSegment,
            StringComparison.CurrentCultureIgnoreCase
        );
        var isTokenRequest = request.Url.AbsolutePath.Contains(
            TokenPathSegment,
            StringComparison.CurrentCultureIgnoreCase
        );

        if (isSwaggerRequest || isTokenRequest)
            return;

        var auditEvent = new AuditEvent
        {
            EventName = "Incoming request - Pre-auth",
            Timestamp = timeProvider.GetUtcNow(),
            CorrelationId = correlationId,
            TraceParent = context.TraceContext.TraceParent,
            Method = request.Method,
            Url = request.Url.AbsolutePath,
        };

        try
        {
            await auditService.SendAuditEventAsync(auditEvent);
        }
        catch (Exception)
        {
            context.GetInvocationResult().Value = await HttpResponseUtility.ProblemResponse(
                request,
                HttpStatusCode.InternalServerError,
                "Audit error",
                $"An error occurred while attempting to audit incoming request. CorrelationId: {correlationId}"
            );
        }
    }

    private async Task AuditOutgoingResponse(FunctionContext context, string correlationId)
    {
        var response = context.GetHttpResponseData();
        if (response is null)
            return;

        var auditEvent = new AuditEvent
        {
            EventName = "Outgoing response",
            Timestamp = timeProvider.GetUtcNow(),
            CorrelationId = correlationId,
            TraceParent = context.TraceContext.TraceParent,
            StatusCode = response.StatusCode,
        };

        try
        {
            await auditService.SendAuditEventAsync(auditEvent);
        }
        catch (Exception)
        {
            var httpRequestDataAsync = await context.GetHttpRequestDataAsync();

            context.GetInvocationResult().Value = await HttpResponseUtility.ProblemResponse(
                httpRequestDataAsync!,
                HttpStatusCode.InternalServerError,
                "Audit error",
                $"An error occurred while attempting to audit outgoing response.  CorrelationId: {correlationId}"
            );
        }
    }
}
