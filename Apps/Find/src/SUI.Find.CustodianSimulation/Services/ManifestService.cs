using System.Diagnostics.CodeAnalysis;
using System.Net;
using Microsoft.Azure.Functions.Worker.Http;
using SUI.Find.CustodianSimulation.Interfaces;
using SUI.Find.CustodianSimulation.Models;

namespace SUI.Find.CustodianSimulation.Services;

[ExcludeFromCodeCoverage(Justification = "Mocked simulator")]
internal static class ManifestService
{
    internal static async Task<HttpResponseData> HandleAsync(
        HttpRequestData req,
        string orgId,
        string personId,
        string? recordType,
        IDataProvider store,
        CancellationToken cancellationToken
    )
    {
        var auth = TryGetAuthContext(req);
        if (auth is null)
        {
            return await ProblemResponse(
                req,
                HttpStatusCode.Unauthorized,
                "Unauthorised",
                "Missing or invalid bearer token."
            );
        }

        if (string.IsNullOrWhiteSpace(personId))
        {
            return await ProblemResponse(
                req,
                HttpStatusCode.BadRequest,
                "Invalid request",
                "personId is required."
            );
        }

        IReadOnlyList<CustodianRecord> records;
        try
        {
            records = string.IsNullOrWhiteSpace(recordType)
                ? await store.GetRecordsAsync(orgId, personId, cancellationToken)
                : await store.GetRecordsAsync(orgId, recordType, personId, cancellationToken);
        }
        catch (FileNotFoundException)
        {
            return await ProblemResponse(
                req,
                HttpStatusCode.NotFound,
                "Not found",
                $"Custodian '{orgId}' is not configured."
            );
        }

        var baseUrl = GetBaseUrl(req);

        var items = records
            .Select(r => new SearchResultItem(
                orgId,
                orgId,
                r.RecordType,
                RecordUrl: BuildRecordUrl(baseUrl, orgId, r.RecordType, r.RecordId)
            ))
            .ToList();

        var res = req.CreateResponse(HttpStatusCode.OK);
        await res.WriteAsJsonAsync(items, cancellationToken);
        return res;
    }

    internal static string? GetQueryParam(HttpRequestData req, string name)
    {
        var query = System.Web.HttpUtility.ParseQueryString(req.Url.Query);
        return query[name];
    }

    internal static string BuildRecordUrl(
        string baseUrl,
        string orgId,
        string recordType,
        string recordId
    )
    {
        return orgId.ToUpperInvariant() switch
        {
            "LOCAL-AUTHORITY-01" =>
                $"{baseUrl}/v1/local-authority/records/{Uri.EscapeDataString(recordId)}?recordType={Uri.EscapeDataString(recordType)}",
            "EDUCATION-01" => $"{baseUrl}/v1/education/records/{Uri.EscapeDataString(recordId)}",
            "HEALTH-01" =>
                $"{baseUrl}/v1/health/children/records/{Uri.EscapeDataString(recordId)}?type={Uri.EscapeDataString(recordType)}",
            "POLICE-01" => $"{baseUrl}/v1/police/records/{Uri.EscapeDataString(recordId)}",
            "HOUSING-01" =>
                $"{baseUrl}/v1/housing/records/{Uri.EscapeDataString(recordType)}/{Uri.EscapeDataString(recordId)}",
            _ =>
                $"{baseUrl}/v1/{orgId.ToLowerInvariant()}/records/{Uri.EscapeDataString(recordId)}",
        };
    }

    internal static async Task<T?> ReadBodyAsync<T>(
        HttpRequestData req,
        CancellationToken cancellationToken
    )
    {
        try
        {
            return await req.ReadFromJsonAsync<T>(cancellationToken: cancellationToken);
        }
        catch
        {
            return default;
        }
    }

    private static string GetBaseUrl(HttpRequestData req)
    {
        var host = req.Headers.TryGetValues("X-Forwarded-Host", out var fwdHost)
            ? fwdHost.FirstOrDefault()
            : req.Url.Host;

        var proto = req.Headers.TryGetValues("X-Forwarded-Proto", out var fwdProto)
            ? fwdProto.FirstOrDefault()
            : req.Url.Scheme;

        var portPart = req.Url.IsDefaultPort ? "" : $":{req.Url.Port}";
        return $"{proto}://{host}{portPart}";
    }

    private static AuthContext? TryGetAuthContext(HttpRequestData req)
    {
        if (!req.Headers.TryGetValues("Authorization", out var authHeaders))
        {
            return null;
        }

        var header = authHeaders.FirstOrDefault();
        if (
            string.IsNullOrWhiteSpace(header)
            || !header.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase)
        )
        {
            return null;
        }

        try
        {
            return AuthContextFactory.FromAuthorizationHeader(header);
        }
        catch
        {
            return null;
        }
    }

    private static async Task<HttpResponseData> ProblemResponse(
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
