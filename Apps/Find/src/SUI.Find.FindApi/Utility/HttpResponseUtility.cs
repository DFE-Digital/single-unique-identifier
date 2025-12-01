using System.Net;
using Microsoft.Azure.Functions.Worker.Http;
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
        await res.WriteAsJsonAsync(
            new Problem("about:blank", title, (int)code, detail, instance),
            cancellationToken
        );
        return res;
    }

    public static async Task<HttpResponseData> UnauthorizedResponse(
        HttpRequestData req,
        string traceId,
        CancellationToken cancellationToken = default
    )
    {
        var res = req.CreateResponse(HttpStatusCode.Unauthorized);
        await res.WriteAsJsonAsync(
            new Problem(
                "about:blank",
                "Unauthorised",
                401,
                "Missing or invalid bearer token.",
                $"traceId:{traceId}"
            ),
            cancellationToken
        );
        return res;
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
}
