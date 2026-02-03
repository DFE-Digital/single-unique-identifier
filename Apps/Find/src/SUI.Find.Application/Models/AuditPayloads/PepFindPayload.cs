namespace SUI.Find.Application.Models.AuditPayloads;

public record PepFindPayload
{
    // The information we need should be able to answer the following questions:
    // If an organisation sais they were missing some information when making a decision after findings records, how do we prove that they did or did not receive it?
    // If an auditor wants to see what information was shared with whom. How can we show them with all the necessary details to be able to know exactly who got what and why?

    public required string DestinationOrgId { get; init; } // Who requested the records
    public required string Purpose { get; init; } // Why the records were requested
    public required string Mode { get; init; } // "EXISTENCE" or "CONTENT"
    public required PepFindRecordDetail[] Records { get; init; } // Details per record
    public required int TotalRecordsFound { get; init; }
    public required int TotalRecordsShared { get; init; }
}
