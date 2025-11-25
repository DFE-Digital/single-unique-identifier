using System.Diagnostics.CodeAnalysis;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Configurations;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Enums;
using Microsoft.OpenApi.Models;

namespace SUI.Find.FindApi.OpenApi;

[ExcludeFromCodeCoverage(
    Justification = "OpenAPI configuration does not contain any logic to be tested."
)]
public sealed class FindOpenApiOptions : DefaultOpenApiConfigurationOptions
{
    public FindOpenApiOptions()
    {
        DocumentFilters.Add(new FindDocumentFilter());
    }

    public override OpenApiInfo Info { get; set; } =
        new()
        {
            Title = "Find a Record API",
            Version = "1",
            Description =
                "The Find a Record API is part of the Single Unique Identifier (SUI) programme\n"
                + "for children’s social care.\n\n"
                + "It provides a minimal asynchronous interface to discover which systems hold\n"
                + "records associated with a given SUI.\n\n"
                + "Capabilities:\n"
                + "- Start a search job for a SUI.\n"
                + "- Poll the status of a search job.\n"
                + "- Retrieve results (endpoints where records can be obtained).\n"
                + "- Cancel an in-progress search job.\n\n"
                + "Deliberately out of scope:\n"
                + "- No demographic or name-based search.\n"
                + "- No record content is returned, only endpoints.\n"
                + "- No purpose/policy headers, DSA identifiers, or client-supplied idempotency keys.",
        };

    public override OpenApiVersionType OpenApiVersion { get; set; } = OpenApiVersionType.V3;
}
