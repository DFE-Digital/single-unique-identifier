using System;

namespace SUI.Find.Infrastructure.Models;

public record SearchRequestMessage
{
    public Guid WorkItemId { get; set; }

    public required string PersonId { get; set; }

    public required string SearchingOrganisationId { get; set; }

    public string? TraceParent { get; set; }

    public required string TraceId { get; set; }

    public required string InvocationId { get; set; }
}
