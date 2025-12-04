using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Configurations;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Enums;
using Microsoft.OpenApi.Models;

namespace SUI.Find.CustodianSimulation.OpenApi;

public sealed class CustodianSimulatorOpenApiOptions : DefaultOpenApiConfigurationOptions
{
    public CustodianSimulatorOpenApiOptions()
    {
        DocumentFilters.Add(new FindDocumentFilter());
    }

    public override OpenApiInfo Info { get; set; } =
        new OpenApiInfo
        {
            Title = "Custodian Simulator API",
            Version = "1",
            Description =
                "The Custodian Simulator API is part of the Single Unique Identifier (SUI) programme.\n\n"
                + "It simulates the external custodian interfaces that FIND will call during discovery and retrieval.\n\n"
                + "Capabilities:\n"
                + "- Answer whether the custodian holds any records for a given SUI.\n"
                + "- Return record pointers (URLs) that can be used to retrieve those records later.\n"
                + "- Serve record content when a returned pointer is dereferenced.\n\n"
                + "Deliberately out of scope:\n"
                + "- No demographic or name-based matching.\n"
                + "- No cross-custodian search or aggregation.\n"
                + "- No purpose/policy enforcement, DSA evaluation, or advanced authorisation.\n"
                + "- No guarantees about data freshness beyond what is modelled for test scenarios.",
        };

    public override OpenApiVersionType OpenApiVersion { get; set; } = OpenApiVersionType.V3;
}
