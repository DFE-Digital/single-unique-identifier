using SUI.Find.Application.Models;

namespace SUI.Find.Application.Dtos;

public record SearchResultEntry : SearchResultItem
{
    /// <summary>
    /// Submitting custodian's Org ID
    /// </summary>
    public required string CustodianId { get; init; }

    public DateTimeOffset SubmittedAtUtc { get; init; }
    public required string JobId { get; init; }
    public required string WorkItemId { get; init; }
    public required string SearchingOrganisationId { get; init; }
}
