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
        string detail
    )
    {
        var res = req.CreateResponse(code);
        await res.WriteAsJsonAsync(new Problem("about:blank", title, (int)code, detail, null));
        return res;
    }
}
