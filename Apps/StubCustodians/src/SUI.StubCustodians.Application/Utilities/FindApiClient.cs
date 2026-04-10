using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using SUI.StubCustodians.Application.Interfaces;
using SUI.StubCustodians.Application.Models;

namespace SUI.StubCustodians.Application.Utilities;

public class FindApiClient : IFindApiClient
{
    private readonly HttpClient _http;

    public FindApiClient(HttpClient http)
    {
        _http = http;
    }

    public async Task<JobInfo?> ClaimAsync(string token)
    {
        using var req = new HttpRequestMessage(HttpMethod.Post, "/api/v2/work/claim");
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        using var res = await _http.SendAsync(req);

        if (res.StatusCode == HttpStatusCode.NoContent)
        {
            return null;
        }

        res.EnsureSuccessStatusCode();

        var json = await res.Content.ReadAsStringAsync();

        return JsonSerializer.Deserialize<JobInfo>(json, JsonSerializerOptions.Web);
    }

    public async Task<RenewJobLeaseResponse?> ExtendLeaseAsync(
        string token,
        RenewJobLeaseRequest request
    )
    {
        using var req = new HttpRequestMessage(HttpMethod.Post, "/api/v2/work/lease/renew");
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        using var content = JsonContent.Create(request);
        await content.LoadIntoBufferAsync(); // Local only concern, stops the HTTP client using chunked transfer encoding, which is not supported by the receiving end (Azure Functions Core Tools local dev host)

        req.Content = content;
        using var res = await _http.SendAsync(req);

        if (res.StatusCode == HttpStatusCode.NoContent)
        {
            return null;
        }

        res.EnsureSuccessStatusCode();

        var json = await res.Content.ReadAsStringAsync();

        return JsonSerializer.Deserialize<RenewJobLeaseResponse>(json, JsonSerializerOptions.Web);
    }

    public async Task SubmitAsync(string token, SubmitJobResultsRequest request)
    {
        using var req = new HttpRequestMessage(HttpMethod.Post, "/api/v2/work/result");
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        using var content = JsonContent.Create(request);
        await content.LoadIntoBufferAsync(); // Local only concern, stops the HTTP client using chunked transfer encoding, which is not supported by the receiving end (Azure Functions Core Tools local dev host)

        req.Content = content;

        using var res = await _http.SendAsync(req);

        if (res.StatusCode == HttpStatusCode.Conflict)
        {
            return;
        }

        res.EnsureSuccessStatusCode();
    }
}
