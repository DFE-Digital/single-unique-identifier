using System.Diagnostics.CodeAnalysis;
using System.Net;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Attributes;
using Microsoft.OpenApi.Models;
using SUI.Find.CustodianSimulation.Interfaces;
using SUI.Find.CustodianSimulation.Models;
using SUI.Find.CustodianSimulation.Services;

namespace SUI.Find.CustodianSimulation.Functions;

[ExcludeFromCodeCoverage(Justification = "Mocked simulator")]
public sealed class LocalAuthorityRecordFunction(IDataProvider store)
{
    private const string OrgId = "LOCAL-AUTHORITY-01";

    [Function("LocalAuthorityRecord")]
    [RequiredScopes("fetch-record.read")]
    [OpenApiOperation(
        operationId: "LocalAuthorityRecord",
        tags: [OrgId],
        Summary = "Get a specific local authority record",
        Description = "GET endpoint; recordType is optional query param, recordId is path param."
    )]
    [OpenApiParameter(
        "recordId",
        In = ParameterLocation.Path,
        Required = true,
        Type = typeof(string)
    )]
    [OpenApiParameter(
        "recordType",
        In = ParameterLocation.Query,
        Required = false,
        Type = typeof(string)
    )]
    [OpenApiResponseWithBody(HttpStatusCode.OK, "application/json", typeof(CustodianRecord))]
    [OpenApiResponseWithBody(HttpStatusCode.NotFound, "application/json", typeof(Problem))]
    [OpenApiResponseWithBody(HttpStatusCode.Unauthorized, "application/json", typeof(Problem))]
    public async Task<HttpResponseData> Run(
        [HttpTrigger(
            AuthorizationLevel.Anonymous,
            "get",
            Route = "v1/local-authority/records/{recordId}"
        )]
            HttpRequestData req,
        string recordId,
        FunctionContext context
    )
    {
        var recordType = ManifestService.GetQueryParam(req, "recordType");

        var record = await store.GetRecordByIdAsync(OrgId, recordId, context.CancellationToken);

        if (
            record is null
            || (
                !string.IsNullOrWhiteSpace(recordType)
                && !string.Equals(record.RecordType, recordType, StringComparison.OrdinalIgnoreCase)
            )
        )
        {
            var notFound = req.CreateResponse(HttpStatusCode.NotFound);
            await notFound.WriteAsJsonAsync(
                new Problem("about:blank", "Not found", 404, "Record not found.", null)
            );
            return notFound;
        }

        var ok = req.CreateResponse(HttpStatusCode.OK);
        await ok.WriteAsJsonAsync(record);
        return ok;
    }
}

[ExcludeFromCodeCoverage(Justification = "Mocked simulator")]
public sealed class EducationRecordFunction(IDataProvider store)
{
    private const string OrgId = "EDUCATION-01";

    [Function("EducationRecord")]
    [RequiredScopes("fetch-record.read")]
    [OpenApiOperation(
        operationId: "EducationRecord",
        tags: [OrgId],
        Summary = "Get a specific education record",
        Description = "GET endpoint; recordId is path param."
    )]
    [OpenApiParameter(
        "recordId",
        In = ParameterLocation.Path,
        Required = true,
        Type = typeof(string)
    )]
    [OpenApiResponseWithBody(HttpStatusCode.OK, "application/json", typeof(CustodianRecord))]
    [OpenApiResponseWithBody(HttpStatusCode.NotFound, "application/json", typeof(Problem))]
    [OpenApiResponseWithBody(HttpStatusCode.Unauthorized, "application/json", typeof(Problem))]
    public async Task<HttpResponseData> Run(
        [HttpTrigger(
            AuthorizationLevel.Anonymous,
            "get",
            Route = "v1/education/records/{recordId}"
        )]
            HttpRequestData req,
        string recordId,
        FunctionContext context
    )
    {
        var record = await store.GetRecordByIdAsync(OrgId, recordId, context.CancellationToken);

        if (record is null)
        {
            var notFound = req.CreateResponse(HttpStatusCode.NotFound);
            await notFound.WriteAsJsonAsync(
                new Problem("about:blank", "Not found", 404, "Record not found.", null)
            );
            return notFound;
        }

        var ok = req.CreateResponse(HttpStatusCode.OK);
        await ok.WriteAsJsonAsync(record);
        return ok;
    }
}

[ExcludeFromCodeCoverage(Justification = "Mocked simulator")]
public sealed class HealthRecordFunction(IDataProvider store)
{
    private const string OrgId = "HEALTH-01";

    [Function("HealthRecord")]
    [RequiredScopes("fetch-record.read")]
    [OpenApiOperation(
        operationId: "HealthRecord",
        tags: [OrgId],
        Summary = "Get a specific health record",
        Description = "GET endpoint; recordType is optional 'type' query param, recordId is path param."
    )]
    [OpenApiParameter(
        "recordId",
        In = ParameterLocation.Path,
        Required = true,
        Type = typeof(string)
    )]
    [OpenApiParameter(
        "type",
        In = ParameterLocation.Query,
        Required = false,
        Type = typeof(string)
    )]
    [OpenApiResponseWithBody(HttpStatusCode.OK, "application/json", typeof(CustodianRecord))]
    [OpenApiResponseWithBody(HttpStatusCode.NotFound, "application/json", typeof(Problem))]
    [OpenApiResponseWithBody(HttpStatusCode.Unauthorized, "application/json", typeof(Problem))]
    public async Task<HttpResponseData> Run(
        [HttpTrigger(
            AuthorizationLevel.Anonymous,
            "get",
            Route = "v1/health/children/records/{recordId}"
        )]
            HttpRequestData req,
        string recordId,
        FunctionContext context
    )
    {
        var recordType = ManifestService.GetQueryParam(req, "type");

        var record = await store.GetRecordByIdAsync(OrgId, recordId, context.CancellationToken);

        if (
            record is null
            || (
                !string.IsNullOrWhiteSpace(recordType)
                && !string.Equals(record.RecordType, recordType, StringComparison.OrdinalIgnoreCase)
            )
        )
        {
            var notFound = req.CreateResponse(HttpStatusCode.NotFound);
            await notFound.WriteAsJsonAsync(
                new Problem("about:blank", "Not found", 404, "Record not found.", null)
            );
            return notFound;
        }

        var ok = req.CreateResponse(HttpStatusCode.OK);
        await ok.WriteAsJsonAsync(record);
        return ok;
    }
}

[ExcludeFromCodeCoverage(Justification = "Mocked simulator")]
public sealed class PoliceRecordFunction(IDataProvider store)
{
    private const string OrgId = "POLICE-01";

    [Function("PoliceRecord")]
    [RequiredScopes("fetch-record.read")]
    [OpenApiOperation(
        operationId: "PoliceRecord",
        tags: [OrgId],
        Summary = "Get a specific police record",
        Description = "GET endpoint; recordId is path param."
    )]
    [OpenApiParameter(
        "recordId",
        In = ParameterLocation.Path,
        Required = true,
        Type = typeof(string)
    )]
    [OpenApiResponseWithBody(HttpStatusCode.OK, "application/json", typeof(CustodianRecord))]
    [OpenApiResponseWithBody(HttpStatusCode.NotFound, "application/json", typeof(Problem))]
    [OpenApiResponseWithBody(HttpStatusCode.Unauthorized, "application/json", typeof(Problem))]
    public async Task<HttpResponseData> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "v1/police/records/{recordId}")]
            HttpRequestData req,
        string recordId,
        FunctionContext context
    )
    {
        var record = await store.GetRecordByIdAsync(OrgId, recordId, context.CancellationToken);

        if (record is null)
        {
            var notFound = req.CreateResponse(HttpStatusCode.NotFound);
            await notFound.WriteAsJsonAsync(
                new Problem("about:blank", "Not found", 404, "Record not found.", null)
            );
            return notFound;
        }

        var ok = req.CreateResponse(HttpStatusCode.OK);
        await ok.WriteAsJsonAsync(record);
        return ok;
    }
}

[ExcludeFromCodeCoverage(Justification = "Mocked simulator")]
public sealed class HousingRecordFunction(IDataProvider store)
{
    private const string OrgId = "HOUSING-01";

    [Function("HousingRecord")]
    [RequiredScopes("fetch-record.read")]
    [OpenApiOperation(
        operationId: "HousingRecord",
        tags: [OrgId],
        Summary = "Get a specific housing record",
        Description = "GET endpoint; optional recordType path segment like the manifest."
    )]
    [OpenApiParameter(
        "recordType",
        In = ParameterLocation.Path,
        Required = false,
        Type = typeof(string)
    )]
    [OpenApiParameter(
        "recordId",
        In = ParameterLocation.Path,
        Required = true,
        Type = typeof(string)
    )]
    [OpenApiResponseWithBody(HttpStatusCode.OK, "application/json", typeof(CustodianRecord))]
    [OpenApiResponseWithBody(HttpStatusCode.NotFound, "application/json", typeof(Problem))]
    [OpenApiResponseWithBody(HttpStatusCode.Unauthorized, "application/json", typeof(Problem))]
    public async Task<HttpResponseData> Run(
        [HttpTrigger(
            AuthorizationLevel.Anonymous,
            "get",
            Route = "v1/housing/records/{recordType?}/{recordId}"
        )]
            HttpRequestData req,
        string? recordType,
        string recordId,
        FunctionContext context
    )
    {
        var record = await store.GetRecordByIdAsync(OrgId, recordId, context.CancellationToken);

        if (
            record is null
            || (
                !string.IsNullOrWhiteSpace(recordType)
                && !string.Equals(record.RecordType, recordType, StringComparison.OrdinalIgnoreCase)
            )
        )
        {
            var notFound = req.CreateResponse(HttpStatusCode.NotFound);
            await notFound.WriteAsJsonAsync(
                new Problem("about:blank", "Not found", 404, "Record not found.", null)
            );
            return notFound;
        }

        var ok = req.CreateResponse(HttpStatusCode.OK);
        await ok.WriteAsJsonAsync(record);
        return ok;
    }
}
