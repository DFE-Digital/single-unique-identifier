namespace SUI.Find.Domain.ValueObjects.Audit;

public abstract record PepAuditPayload
{
    public required string EndpointType { get; init; } // "FIND" or "FETCH"
    public required string EndpointUrl { get; init; } // e.g. "/search/123/results"

    public int TotalItemsRequested { get; init; }
    public int TotalItemsReturned { get; init; }

    public string[] DeniedUrls { get; init; } = [];

    public string[] AcceptedUrls { get; init; } = [];
}
