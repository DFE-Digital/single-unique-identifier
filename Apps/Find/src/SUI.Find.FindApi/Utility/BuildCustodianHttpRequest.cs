using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using SUI.Find.Application.Models;

namespace SUI.Find.FindApi.Utility;

public static class BuildCustodianHttpRequest
{
    public static HttpRequestMessage BuildHttpRequest(ProviderDefinition provider, string encryptedPersonId, string? bearerToken)
    {
        var connectionDefinition = provider.Connection;
        var method = new HttpMethod(connectionDefinition.Method.ToUpperInvariant());

        var url = MapPersonIdToUrl(connectionDefinition.Url, encryptedPersonId, connectionDefinition.PersonIdPosition);

        var request = new HttpRequestMessage(method, url);

        request.Headers.Add("orgId", provider.OrgId);

        if (!string.IsNullOrWhiteSpace(bearerToken))
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", bearerToken);
        }

        if (string.Equals(connectionDefinition.PersonIdPosition, "header", StringComparison.OrdinalIgnoreCase))
        {
            request.Headers.Add("personId", encryptedPersonId);
        }

        if (method != HttpMethod.Post && method != HttpMethod.Put && method != HttpMethod.Patch) return request;

        var body = BuildHttpBody(connectionDefinition, encryptedPersonId);

        if (body is not null)
        {
            request.Content = new StringContent(body, Encoding.UTF8, "application/json");
        }

        return request;
    }

    private static string MapPersonIdToUrl(string url, string encryptedPersonId, string position)
    {
        if (string.Equals(position, "path", StringComparison.OrdinalIgnoreCase))
        {
            return url.Replace("{personId}", Uri.EscapeDataString(encryptedPersonId), StringComparison.OrdinalIgnoreCase);
        }

        if (!string.Equals(position, "query", StringComparison.OrdinalIgnoreCase)) return url;

        var separator = url.Contains('?') ? "&" : "?";

        return $"{url}{separator}personId={Uri.EscapeDataString(encryptedPersonId)}";
    }

    private static string? BuildHttpBody(ConnectionDefinition connectionDefinition, string personId)
    {
        if (!string.IsNullOrWhiteSpace(connectionDefinition.BodyTemplateJson))
        {
            return connectionDefinition.BodyTemplateJson.Replace("{personId}", personId, StringComparison.OrdinalIgnoreCase);
        }

        return string.Equals(connectionDefinition.PersonIdPosition, "body", StringComparison.OrdinalIgnoreCase) ? JsonSerializer.Serialize(new { personId }) : null;
    }
}
