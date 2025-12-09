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
public sealed class LocalAuthorityManifestFunction(IDataProvider store)
{
    private const string OrgId = "LOCAL-AUTHORITY-01";

    [Function("LocalAuthorityManifest")]
    [RequiredScopes("find-record.read")]
    [OpenApiOperation(
        operationId: "LocalAuthorityManifest",
        tags: [OrgId],
        Summary = "Local authority manifest lookup",
        Description = "GET endpoint; optional recordType query param."
    )]
    [OpenApiParameter(
        "personId",
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
    [OpenApiResponseWithBody(
        HttpStatusCode.OK,
        "application/json",
        typeof(IEnumerable<SearchResultItem>)
    )]
    [OpenApiResponseWithBody(HttpStatusCode.BadRequest, "application/json", typeof(Problem))]
    [OpenApiResponseWithBody(HttpStatusCode.NotFound, "application/json", typeof(Problem))]
    [OpenApiResponseWithBody(HttpStatusCode.Unauthorized, "application/json", typeof(Problem))]
    public Task<HttpResponseData> Run(
        [HttpTrigger(
            AuthorizationLevel.Function,
            "get",
            Route = "v1/local-authority/manifest/{personId}"
        )]
            HttpRequestData req,
        string personId,
        FunctionContext context
    )
    {
        var recordType = ManifestService.GetQueryParam(req, "recordType");
        return ManifestService.HandleAsync(
            req,
            OrgId,
            personId,
            recordType,
            store,
            context.CancellationToken
        );
    }
}

[ExcludeFromCodeCoverage(Justification = "Mocked simulator")]
public sealed record EducationManifestRequest(string PersonId, string? RecordType);

[ExcludeFromCodeCoverage(Justification = "Mocked simulator")]
public sealed class EducationManifestFunction(IDataProvider store)
{
    private const string OrgId = "EDUCATION-01";

    [Function("EducationManifest")]
    [RequiredScopes("find-record.read")]
    [OpenApiOperation(
        operationId: "EducationManifest",
        tags: [OrgId],
        Summary = "Education manifest lookup",
        Description = "POST endpoint; personId and optional recordType supplied in JSON body."
    )]
    [OpenApiRequestBody("application/json", typeof(EducationManifestRequest), Required = true)]
    [OpenApiResponseWithBody(
        HttpStatusCode.OK,
        "application/json",
        typeof(IEnumerable<SearchResultItem>)
    )]
    [OpenApiResponseWithBody(HttpStatusCode.BadRequest, "application/json", typeof(Problem))]
    [OpenApiResponseWithBody(HttpStatusCode.NotFound, "application/json", typeof(Problem))]
    [OpenApiResponseWithBody(HttpStatusCode.Unauthorized, "application/json", typeof(Problem))]
    public async Task<HttpResponseData> Run(
        [HttpTrigger(AuthorizationLevel.Function, "post", Route = "v1/education/manifest")]
            HttpRequestData req,
        FunctionContext context
    )
    {
        var body = await ManifestService.ReadBodyAsync<EducationManifestRequest>(
            req,
            context.CancellationToken
        );
        var personId = body?.PersonId ?? string.Empty;
        var recordType = body?.RecordType;
        return await ManifestService.HandleAsync(
            req,
            OrgId,
            personId,
            recordType,
            store,
            context.CancellationToken
        );
    }
}

[ExcludeFromCodeCoverage(Justification = "Mocked simulator")]
public sealed class HealthManifestFunction(IDataProvider store)
{
    private const string OrgId = "HEALTH-01";

    [Function("HealthManifest")]
    [RequiredScopes("find-record.read")]
    [OpenApiOperation(
        operationId: "HealthManifest",
        tags: [OrgId],
        Summary = "Health manifest lookup",
        Description = "GET endpoint; optional type query param for recordType."
    )]
    [OpenApiParameter(
        "personId",
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
    [OpenApiResponseWithBody(
        HttpStatusCode.OK,
        "application/json",
        typeof(IEnumerable<SearchResultItem>)
    )]
    [OpenApiResponseWithBody(HttpStatusCode.BadRequest, "application/json", typeof(Problem))]
    [OpenApiResponseWithBody(HttpStatusCode.NotFound, "application/json", typeof(Problem))]
    [OpenApiResponseWithBody(HttpStatusCode.Unauthorized, "application/json", typeof(Problem))]
    public Task<HttpResponseData> Run(
        [HttpTrigger(
            AuthorizationLevel.Function,
            "get",
            Route = "v1/health/children/{personId}/manifest"
        )]
            HttpRequestData req,
        string personId,
        FunctionContext context
    )
    {
        var recordType = ManifestService.GetQueryParam(req, "type");
        return ManifestService.HandleAsync(
            req,
            OrgId,
            personId,
            recordType,
            store,
            context.CancellationToken
        );
    }
}

[ExcludeFromCodeCoverage(Justification = "Mocked simulator")]
public sealed class PoliceManifestFunction(IDataProvider store)
{
    private const string OrgId = "POLICE-01";
    private const string FixedRecordType = "crime-justice";

    [Function("PoliceManifest")]
    [RequiredScopes("find-record.read")]
    [OpenApiOperation(
        operationId: "PoliceManifest",
        tags: [OrgId],
        Summary = "Police manifest lookup",
        Description = "POST endpoint; recordType is fixed to crime-justice."
    )]
    [OpenApiParameter(
        "personId",
        In = ParameterLocation.Path,
        Required = true,
        Type = typeof(string)
    )]
    [OpenApiResponseWithBody(
        HttpStatusCode.OK,
        "application/json",
        typeof(IEnumerable<SearchResultItem>)
    )]
    [OpenApiResponseWithBody(HttpStatusCode.BadRequest, "application/json", typeof(Problem))]
    [OpenApiResponseWithBody(HttpStatusCode.NotFound, "application/json", typeof(Problem))]
    [OpenApiResponseWithBody(HttpStatusCode.Unauthorized, "application/json", typeof(Problem))]
    public Task<HttpResponseData> Run(
        [HttpTrigger(AuthorizationLevel.Function, "post", Route = "v1/police/locate/{personId}")]
            HttpRequestData req,
        string personId,
        FunctionContext context
    )
    {
        return ManifestService.HandleAsync(
            req,
            OrgId,
            personId,
            FixedRecordType,
            store,
            context.CancellationToken
        );
    }
}

[ExcludeFromCodeCoverage(Justification = "Mocked simulator")]
public sealed class HousingManifestFunction(IDataProvider store)
{
    private const string OrgId = "HOUSING-01";

    [Function("HousingManifest")]
    [RequiredScopes("find-record.read")]
    [OpenApiOperation(
        operationId: "HousingManifest",
        tags: [OrgId],
        Summary = "Housing manifest lookup",
        Description = "GET endpoint; recordType may be supplied as an optional path segment."
    )]
    [OpenApiParameter(
        "personId",
        In = ParameterLocation.Path,
        Required = true,
        Type = typeof(string)
    )]
    [OpenApiParameter(
        "recordType",
        In = ParameterLocation.Path,
        Required = false,
        Type = typeof(string)
    )]
    [OpenApiResponseWithBody(
        HttpStatusCode.OK,
        "application/json",
        typeof(IEnumerable<SearchResultItem>)
    )]
    [OpenApiResponseWithBody(HttpStatusCode.BadRequest, "application/json", typeof(Problem))]
    [OpenApiResponseWithBody(HttpStatusCode.NotFound, "application/json", typeof(Problem))]
    [OpenApiResponseWithBody(HttpStatusCode.Unauthorized, "application/json", typeof(Problem))]
    public Task<HttpResponseData> Run(
        [HttpTrigger(
            AuthorizationLevel.Function,
            "get",
            Route = "v1/housing/manifest/{personId}/{recordType?}"
        )]
            HttpRequestData req,
        string personId,
        string? recordType,
        FunctionContext context
    )
    {
        return ManifestService.HandleAsync(
            req,
            OrgId,
            personId,
            recordType,
            store,
            context.CancellationToken
        );
    }
}
