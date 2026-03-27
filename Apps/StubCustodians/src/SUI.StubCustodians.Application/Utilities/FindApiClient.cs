using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using SUI.StubCustodians.Application.Models;

namespace SUI.StubCustodians.Application.Utilities;

public class FindApiClient
{
    private readonly HttpClient _http;

    public FindApiClient(HttpClient http)
    {
        _http = http;
    }

    public async Task<JobInfo?> ClaimAsync(string token)
    {
        var req = new HttpRequestMessage(HttpMethod.Post, "/v2/work/claim");
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var res = await _http.SendAsync(req);

        if (res.StatusCode == HttpStatusCode.NoContent)
            return null;

        res.EnsureSuccessStatusCode();

        var json = await res.Content.ReadAsStringAsync();

        return JsonSerializer.Deserialize<JobInfo>(json, JsonSerializerOptions.Web);
    }

    public async Task SubmitAsync(string token, SubmitJobResultsRequest request)
    {
        var req = new HttpRequestMessage(HttpMethod.Post, "/v2/work/result");
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        req.Content = JsonContent.Create(request);

        var res = await _http.SendAsync(req);

        if (res.StatusCode == HttpStatusCode.Conflict)
        {
            return;
        }

        res.EnsureSuccessStatusCode();
    }
}
