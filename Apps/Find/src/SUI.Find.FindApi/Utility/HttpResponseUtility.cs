using System.Net;
using Microsoft.Azure.Functions.Worker.Http;
using SUI.Find.Application.Constants;
using SUI.Find.FindApi.Models;

namespace SUI.Find.FindApi.Utility;

public static class HttpResponseUtility
{
    public static async Task<HttpResponseData> ProblemResponse(
        HttpRequestData req,
        HttpStatusCode code,
        string title,
        string detail,
        string? instance = null,
        CancellationToken cancellationToken = default
    )
    {
        var res = req.CreateResponse(code);
        res.AddNoCacheHeaders();
        await res.WriteAsJsonAsync(
            new Problem("about:blank", title, (int)code, detail, instance),
            cancellationToken
        );
        return res;
    }

    public static async Task<HttpResponseData> ForbiddenResponse(
        HttpRequestData req,
        string traceId,
        CancellationToken cancellationToken = default
    )
    {
        var res = ProblemResponse(
            req,
            HttpStatusCode.Forbidden,
            "Access Denied",
            "No privileges to access resource.",
            $"urn:trace::{traceId}",
            cancellationToken
        );

        return await res;
    }

    public static async Task<HttpResponseData> UnauthorizedResponse(
        HttpRequestData req,
        string traceId,
        CancellationToken cancellationToken = default
    )
    {
        var res = ProblemResponse(
            req,
            HttpStatusCode.Unauthorized,
            "Unauthorised",
            "Missing or invalid bearer token.",
            $"urn:trace::{traceId}",
            cancellationToken
        );

        return await res;
    }

    public static async Task<HttpResponseData> NotFoundResponse(
        HttpRequestData req,
        string traceId,
        CancellationToken cancellationToken = default
    )
    {
        var res = ProblemResponse(
            req,
            HttpStatusCode.NotFound,
            "Not Found",
            "The requested resource was not found.",
            $"urn:trace:{traceId}",
            cancellationToken
        );
        return await res;
    }

    public static async Task<HttpResponseData> BadRequestResponse(
        HttpRequestData req,
        string traceId,
        string detail,
        string title = "Bad Request",
        CancellationToken cancellationToken = default
    )
    {
        var res = ProblemResponse(
            req,
            HttpStatusCode.BadRequest,
            title,
            detail,
            $"urn:trace:{traceId}",
            cancellationToken
        );
        return await res;
    }

    public static async Task<HttpResponseData> InternalServerErrorResponse(
        HttpRequestData req,
        string? traceId,
        CancellationToken cancellationToken = default
    )
    {
        var res = ProblemResponse(
            req,
            HttpStatusCode.InternalServerError,
            "Internal Server Error",
            "An unexpected error occurred while processing the request.",
            $"urn:trace:{traceId}",
            cancellationToken
        );
        return await res;
    }

    public static async Task<HttpResponseData> CreatedResponse<T>(
        HttpRequestData req,
        T body,
        CancellationToken cancellationToken = default
    )
    {
        var res = req.CreateResponse(HttpStatusCode.Created);
        res.AddNoCacheHeaders();
        await res.WriteAsJsonAsync(body, cancellationToken);
        return res;
    }

    public static async Task<HttpResponseData> AcceptedResponse<T>(
        HttpRequestData req,
        T body,
        CancellationToken cancellationToken = default
    )
    {
        var res = req.CreateResponse(HttpStatusCode.Accepted);
        res.AddNoCacheHeaders();
        await res.WriteAsJsonAsync(body, cancellationToken);
        return res;
    }

    public static HttpResponseData AcceptedResponse(HttpRequestData req)
    {
        var res = req.CreateResponse(HttpStatusCode.Accepted);
        res.AddNoCacheHeaders();
        return res;
    }

    public static async Task<HttpResponseData> OkResponse<T>(
        HttpRequestData req,
        T body,
        CancellationToken cancellationToken = default
    )
    {
        var res = req.CreateResponse(HttpStatusCode.OK);
        res.AddNoCacheHeaders();
        await res.WriteAsJsonAsync(body, cancellationToken);
        return res;
    }

    public static HttpResponseData OkResponse(HttpRequestData req)
    {
        var res = req.CreateResponse(HttpStatusCode.OK);
        res.AddNoCacheHeaders();
        return res;
    }

    public static HttpResponseData NoContentResponse(HttpRequestData req)
    {
        var res = req.CreateResponse(HttpStatusCode.NoContent);
        res.AddNoCacheHeaders();
        return res;
    }

    public static HttpResponseData AddRetryAfterHeader(this HttpResponseData response)
    {
        response.Headers.Add(
            ApplicationConstants.Http.RetryAfterHeaderName,
            ApplicationConstants.Http.DefaultRetryAfterSeconds
        );
        return response;
    }

    private static void AddNoCacheHeaders(this HttpResponseData response)
    {
        response.Headers.Add("Cache-Control", "no-store, no-cache, max-age=0, must-revalidate");
        response.Headers.Add("Pragma", "no-cache");
        response.Headers.Add("Expires", DateTimeOffset.UnixEpoch.ToString("R"));
        response.Headers.Add("Vary", "Authorization");
    }
}
