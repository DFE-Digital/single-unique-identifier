using System.Diagnostics.CodeAnalysis;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Abstractions;
using Microsoft.OpenApi.Models;
using SUI.Find.Application.Constants;

namespace SUI.Find.FindApi.OpenApi;

[ExcludeFromCodeCoverage(Justification = "OpenAPI header does not contain any logic to be tested.")]
public class RetryAfterHeader : IOpenApiCustomResponseHeader
{
    public Dictionary<string, OpenApiHeader> Headers { get; set; } =
        new()
        {
            {
                ApplicationConstants.Http.RetryAfterHeaderName,
                new OpenApiHeader
                {
                    Description =
                        "The minimum number of seconds the client should wait before making a follow-up request.",
                    Schema = new OpenApiSchema { Type = "integer" },
                }
            },
        };
}
