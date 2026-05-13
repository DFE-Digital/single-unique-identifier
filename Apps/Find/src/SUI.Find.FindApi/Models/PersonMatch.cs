using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Attributes;

namespace SUI.Find.FindApi.Models;

public record PersonMatch(
    [property: OpenApiProperty(
        Description = "The Single Unique Identifier for an individual",
        Default = "9449305552",
        Nullable = false
    )]
        string PersonId
);
