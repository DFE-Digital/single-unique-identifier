using Interfaces;
using Microsoft.Azure.Functions.Worker;
using Models;
using SUI.Find.Functions.Interfaces;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

public sealed record QueryProviderInput(string RequestingOrg, string JobId, string Sui, ProviderDefinition Provider);

public sealed class QueryProviderActivity(
    IHttpClientFactory httpClientFactory,
    IPersonIdEncryptionService crypto,
    IOutboundAuthTokenService tokenService,
    IFetchUrlMappingStore fetchUrlMappingStore)
{
    private readonly IHttpClientFactory _httpClientFactory = httpClientFactory;
    private readonly IPersonIdEncryptionService _crypto = crypto;
    private readonly IOutboundAuthTokenService _tokenService = tokenService;
    private readonly IFetchUrlMappingStore _fetchUrlMappingStore = fetchUrlMappingStore;

    [Function("QueryProviderActivity")]
    public async Task<IReadOnlyList<SearchResultItem>> Run(
        [ActivityTrigger] QueryProviderInput input,
        FunctionContext context)
    {
        var provider = input.Provider;

        if (provider.Encryption is null)
        {
            throw new InvalidOperationException($"Provider '{provider.OrgId}' has no encryption configured.");
        }

        var personId = _crypto.EncryptNhsToPersonId(input.Sui, provider.Encryption);

        var auth = provider.Connection.Auth;
        var bearer = auth is null
            ? null
            : await _tokenService.GetAccessTokenAsync(auth, context.CancellationToken);

        using var req = BuildRequest(provider, personId, bearer);

        try
        {
            using var http = _httpClientFactory.CreateClient("providers");
            using var res = await http.SendAsync(req, context.CancellationToken);

            if (!res.IsSuccessStatusCode)
            {
                return Array.Empty<SearchResultItem>();
            }

            var json = await res.Content.ReadAsStringAsync(context.CancellationToken);

            if (string.IsNullOrWhiteSpace(json))
            {
                return Array.Empty<SearchResultItem>();
            }

            var items = JsonSerializer.Deserialize<List<SearchResultItem>>(
                json,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            if (items is null || items.Count == 0)
            {
                return Array.Empty<SearchResultItem>();
            }

            var masked = new List<SearchResultItem>(items.Count);

            foreach (var item in items)
            {
                var mapping = await _fetchUrlMappingStore.CreateAsync(
                    jobId: input.JobId,
                    targetUrl: item.RecordUrl,
                    targetOrg: provider.OrgId,
                    requestingOrg: input.RequestingOrg,
                    recordType: item.RecordType,
                    ttl: TimeSpan.FromMinutes(10),
                    ct: context.CancellationToken);

                var rewritten = item with { RecordUrl = mapping.Url };
                masked.Add(rewritten);
            }

            return masked;
        }
        catch
        {
            return Array.Empty<SearchResultItem>();
        }
    }

    private static HttpRequestMessage BuildRequest(ProviderDefinition provider, string personId, string? bearer)
    {
        var c = provider.Connection;
        var method = new HttpMethod(c.Method.ToUpperInvariant());

        var url = ApplyPersonIdToUrl(c.Url, personId, c.PersonIdPosition);

        var req = new HttpRequestMessage(method, url);

        req.Headers.Add("orgId", provider.OrgId);

        if (!string.IsNullOrWhiteSpace(bearer))
        {
            req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", bearer);
        }

        if (string.Equals(c.PersonIdPosition, "header", StringComparison.OrdinalIgnoreCase))
        {
            req.Headers.Add("personId", personId);
        }

        if (method == HttpMethod.Post || method == HttpMethod.Put || method == HttpMethod.Patch)
        {
            var body = BuildBody(c, personId);
            if (body is not null)
            {
                req.Content = new StringContent(body, Encoding.UTF8, "application/json");
            }
        }

        return req;
    }

    private static string ApplyPersonIdToUrl(string url, string personId, string position)
    {
        if (string.Equals(position, "path", StringComparison.OrdinalIgnoreCase))
        {
            return url.Replace("{personId}", Uri.EscapeDataString(personId), StringComparison.OrdinalIgnoreCase);
        }

        if (string.Equals(position, "query", StringComparison.OrdinalIgnoreCase))
        {
            var separator = url.Contains('?') ? "&" : "?";
            return $"{url}{separator}personId={Uri.EscapeDataString(personId)}";
        }

        return url;
    }

    private static string? BuildBody(ConnectionDefinition c, string personId)
    {
        if (!string.IsNullOrWhiteSpace(c.BodyTemplateJson))
        {
            return c.BodyTemplateJson.Replace("{personId}", personId, StringComparison.OrdinalIgnoreCase);
        }

        if (string.Equals(c.PersonIdPosition, "body", StringComparison.OrdinalIgnoreCase))
        {
            return JsonSerializer.Serialize(new { personId });
        }

        return null;
    }
}
