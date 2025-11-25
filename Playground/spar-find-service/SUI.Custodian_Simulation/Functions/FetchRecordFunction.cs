using System.Net;
using Interfaces;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Attributes;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Enums;
using Microsoft.OpenApi.Models;
using Models;

namespace Functions;

public sealed class LocalAuthorityRecordFunction(IDataProvider store)
{
    private const string OrgId = "LOCAL-AUTHORITY-01";
    private readonly IDataProvider _store = store;

    [Function("LocalAuthorityRecord")]
    [RequiredScopes("fetch-record.read")]
    [OpenApiOperation(
        operationId: "LocalAuthorityRecord",
        tags: new[] { OrgId },
        Summary = "Get a specific local authority record",
        Description = "GET endpoint; recordType is optional query param, recordId is path param.")]
    [OpenApiParameter("recordId", In = ParameterLocation.Path, Required = true, Type = typeof(string))]
    [OpenApiParameter("recordType", In = ParameterLocation.Query, Required = false, Type = typeof(string))]
    [OpenApiResponseWithBody(HttpStatusCode.OK, "application/json", typeof(CustodianRecord))]
    [OpenApiResponseWithBody(HttpStatusCode.NotFound, "application/json", typeof(Problem))]
    [OpenApiResponseWithBody(HttpStatusCode.Unauthorized, "application/json", typeof(Problem))]
    public async Task<HttpResponseData> Run(
        [HttpTrigger(AuthorizationLevel.Function, "get", Route = "v1/local-authority/records/{recordId}")]
        HttpRequestData req,
        string personId,
        string recordId,
        FunctionContext context)
    {
        var recordType = ManifestService.GetQueryParam(req, "recordType");

        var records = string.IsNullOrWhiteSpace(recordType)
            ? await _store.GetRecordsAsync(OrgId, personId, context.CancellationToken)
            : await _store.GetRecordsAsync(OrgId, recordType!, personId, context.CancellationToken);

        var match = records.FirstOrDefault(r => string.Equals(r.RecordId, recordId, StringComparison.OrdinalIgnoreCase));

        if (match is null)
        {
            var notFound = req.CreateResponse(HttpStatusCode.NotFound);
            await notFound.WriteAsJsonAsync(new Problem("about:blank", "Not found", 404, "Record not found.", null));
            return notFound;
        }

        var ok = req.CreateResponse(HttpStatusCode.OK);
        await ok.WriteAsJsonAsync(match);
        return ok;
    }
}

public sealed class EducationRecordFunction(IDataProvider store)
{
    private const string OrgId = "EDUCATION-01";
    private readonly IDataProvider _store = store;

    [Function("EducationRecord")]
    [RequiredScopes("fetch-record.read")]
    [OpenApiOperation(
        operationId: "EducationRecord",
        tags: new[] { OrgId },
        Summary = "Get a specific education record",
        Description = "GET endpoint; recordId is path param.")]
    [OpenApiParameter("recordId", In = ParameterLocation.Path, Required = true, Type = typeof(string))]
    [OpenApiResponseWithBody(HttpStatusCode.OK, "application/json", typeof(CustodianRecord))]
    [OpenApiResponseWithBody(HttpStatusCode.NotFound, "application/json", typeof(Problem))]
    [OpenApiResponseWithBody(HttpStatusCode.Unauthorized, "application/json", typeof(Problem))]
    public async Task<HttpResponseData> Run(
        [HttpTrigger(AuthorizationLevel.Function, "get", Route = "v1/education/records/{recordId}")]
        HttpRequestData req,
        string personId,
        string recordId,
        FunctionContext context)
    {
        var records = await _store.GetRecordsAsync(OrgId, personId, context.CancellationToken);

        var match = records.FirstOrDefault(r => string.Equals(r.RecordId, recordId, StringComparison.OrdinalIgnoreCase));

        if (match is null)
        {
            var notFound = req.CreateResponse(HttpStatusCode.NotFound);
            await notFound.WriteAsJsonAsync(new Problem("about:blank", "Not found", 404, "Record not found.", null));
            return notFound;
        }

        var ok = req.CreateResponse(HttpStatusCode.OK);
        await ok.WriteAsJsonAsync(match);
        return ok;
    }
}

public sealed class HealthRecordFunction(IDataProvider store)
{
    private const string OrgId = "HEALTH-01";
    private readonly IDataProvider _store = store;

    [Function("HealthRecord")]
    [RequiredScopes("fetch-record.read")]
    [OpenApiOperation(
        operationId: "HealthRecord",
        tags: new[] { OrgId },
        Summary = "Get a specific health record",
        Description = "GET endpoint; recordType is optional 'type' query param, recordId is path param.")]
    [OpenApiParameter("recordId", In = ParameterLocation.Path, Required = true, Type = typeof(string))]
    [OpenApiParameter("type", In = ParameterLocation.Query, Required = false, Type = typeof(string))]
    [OpenApiResponseWithBody(HttpStatusCode.OK, "application/json", typeof(CustodianRecord))]
    [OpenApiResponseWithBody(HttpStatusCode.NotFound, "application/json", typeof(Problem))]
    [OpenApiResponseWithBody(HttpStatusCode.Unauthorized, "application/json", typeof(Problem))]
    public async Task<HttpResponseData> Run(
        [HttpTrigger(AuthorizationLevel.Function, "get", Route = "v1/health/children/records/{recordId}")]
        HttpRequestData req,
        string personId,
        string recordId,
        FunctionContext context)
    {
        var recordType = ManifestService.GetQueryParam(req, "type");

        var records = string.IsNullOrWhiteSpace(recordType)
            ? await _store.GetRecordsAsync(OrgId, personId, context.CancellationToken)
            : await _store.GetRecordsAsync(OrgId, recordType!, personId, context.CancellationToken);

        var match = records.FirstOrDefault(r => string.Equals(r.RecordId, recordId, StringComparison.OrdinalIgnoreCase));

        if (match is null)
        {
            var notFound = req.CreateResponse(HttpStatusCode.NotFound);
            await notFound.WriteAsJsonAsync(new Problem("about:blank", "Not found", 404, "Record not found.", null));
            return notFound;
        }

        var ok = req.CreateResponse(HttpStatusCode.OK);
        await ok.WriteAsJsonAsync(match);
        return ok;
    }
}

public sealed class PoliceRecordFunction(IDataProvider store)
{
    private const string OrgId = "POLICE-01";
    private readonly IDataProvider _store = store;

    [Function("PoliceRecord")]
    [RequiredScopes("fetch-record.read")]
    [OpenApiOperation(
        operationId: "PoliceRecord",
        tags: new[] { OrgId },
        Summary = "Get a specific police record",
        Description = "GET endpoint; recordId is path param.")]
    [OpenApiParameter("recordId", In = ParameterLocation.Path, Required = true, Type = typeof(string))]
    [OpenApiResponseWithBody(HttpStatusCode.OK, "application/json", typeof(CustodianRecord))]
    [OpenApiResponseWithBody(HttpStatusCode.NotFound, "application/json", typeof(Problem))]
    [OpenApiResponseWithBody(HttpStatusCode.Unauthorized, "application/json", typeof(Problem))]
    public async Task<HttpResponseData> Run(
        [HttpTrigger(AuthorizationLevel.Function, "get", Route = "v1/police/records/{recordId}")]
        HttpRequestData req,
        string personId,
        string recordId,
        FunctionContext context)
    {
        var records = await _store.GetRecordsAsync(OrgId, personId, context.CancellationToken);

        var match = records.FirstOrDefault(r => string.Equals(r.RecordId, recordId, StringComparison.OrdinalIgnoreCase));

        if (match is null)
        {
            var notFound = req.CreateResponse(HttpStatusCode.NotFound);
            await notFound.WriteAsJsonAsync(new Problem("about:blank", "Not found", 404, "Record not found.", null));
            return notFound;
        }

        var ok = req.CreateResponse(HttpStatusCode.OK);
        await ok.WriteAsJsonAsync(match);
        return ok;
    }
}

public sealed class HousingRecordFunction(IDataProvider store)
{
    private const string OrgId = "HOUSING-01";
    private readonly IDataProvider _store = store;

    [Function("HousingRecord")]
    [RequiredScopes("fetch-record.read")]
    [OpenApiOperation(
        operationId: "HousingRecord",
        tags: new[] { OrgId },
        Summary = "Get a specific housing record",
        Description = "GET endpoint; optional recordType path segment like the manifest.")]
    [OpenApiParameter("recordId", In = ParameterLocation.Path, Required = true, Type = typeof(string))]
    [OpenApiParameter("recordType", In = ParameterLocation.Path, Required = false, Type = typeof(string))]
    [OpenApiResponseWithBody(HttpStatusCode.OK, "application/json", typeof(CustodianRecord))]
    [OpenApiResponseWithBody(HttpStatusCode.NotFound, "application/json", typeof(Problem))]
    [OpenApiResponseWithBody(HttpStatusCode.Unauthorized, "application/json", typeof(Problem))]
    public async Task<HttpResponseData> Run(
        [HttpTrigger(AuthorizationLevel.Function, "get", Route = "v1/housing/records/{recordType?}/{recordId}")]
        HttpRequestData req,
        string personId,
        string? recordType,
        string recordId,
        FunctionContext context)
    {
        var records = string.IsNullOrWhiteSpace(recordType)
            ? await _store.GetRecordsAsync(OrgId, personId, context.CancellationToken)
            : await _store.GetRecordsAsync(OrgId, recordType!, personId, context.CancellationToken);

        var match = records.FirstOrDefault(r => string.Equals(r.RecordId, recordId, StringComparison.OrdinalIgnoreCase));

        if (match is null)
        {
            var notFound = req.CreateResponse(HttpStatusCode.NotFound);
            await notFound.WriteAsJsonAsync(new Problem("about:blank", "Not found", 404, "Record not found.", null));
            return notFound;
        }

        var ok = req.CreateResponse(HttpStatusCode.OK);
        await ok.WriteAsJsonAsync(match);
        return ok;
    }
}
