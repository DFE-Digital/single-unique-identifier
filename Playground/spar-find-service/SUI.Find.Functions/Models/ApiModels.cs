using Azure;
using Azure.Data.Tables;
using System.Text.Json.Serialization;

namespace Models;

public sealed class StartSearchRequest
{
    public string PersonId { get; set; } = string.Empty;
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum SearchStatus
{
    Queued,
    Running,
    Completed,
    Failed,
    Cancelled
}

public sealed record HalLink(string Href, string Method);

public sealed record SearchJobLinks(
    HalLink Self,
    HalLink? Status,
    HalLink? Results,
    HalLink? Cancel
);

public sealed record SearchJob(
    string JobId,
    string PersonId,
    SearchStatus Status,
    string CreatedAt,
    string? LastUpdatedAt,
    SearchJobLinks _Links
);

public sealed record SearchResultItem(
    string ProviderSystem,
    string ProviderName,
    string RecordType,
    string RecordUrl
);

public sealed record SearchResultsLinks(
    HalLink Self,
    HalLink? Job
);

public sealed record SearchResults(
    string JobId,
    string PersonId,
    SearchStatus Status,
    IReadOnlyList<SearchResultItem> Items,
    SearchResultsLinks _Links
);

public sealed record Problem(
    string Type,
    string Title,
    int Status,
    string Detail,
    string? Instance
);

public sealed record HealthStatus(string Status);

public abstract record RecordEnvelopeBase(
    string DataType,
    string RecordId,
    string ProviderSystem,
    string Sui
);

public sealed record LocalAuthorityChildrenSocialCareCaseDataV1(
    string CaseId,
    string LocalAuthorityCode,
    string OpenedAt
);

public sealed record EducationAttendanceRecordDataV1(
    string SchoolUrn,
    string AttendanceStartDate,
    string AttendanceEndDate,
    double AttendanceRate
);

public sealed record LocalAuthorityChildrenSocialCareCaseEnvelopeV1(
    string DataType,
    string RecordId,
    string ProviderSystem,
    string Sui,
    LocalAuthorityChildrenSocialCareCaseDataV1 Data
) : RecordEnvelopeBase(DataType, RecordId, ProviderSystem, Sui);

public sealed record EducationAttendanceRecordEnvelopeV1(
    string DataType,
    string RecordId,
    string ProviderSystem,
    string Sui,
    EducationAttendanceRecordDataV1 Data
) : RecordEnvelopeBase(DataType, RecordId, ProviderSystem, Sui);

public sealed record FindPersonRequest
{
    [JsonPropertyName("given")]
    public string? Given { get; set; }

    [JsonPropertyName("family")]
    public string? Family { get; set; }

    [JsonPropertyName("birthDate")]
    public string? BirthDate { get; set; }

    [JsonPropertyName("gender")]
    public string? Gender { get; set; }

    [JsonPropertyName("phone")]
    public string? Phone { get; set; }

    [JsonPropertyName("email")]
    public string? Email { get; set; }

    [JsonPropertyName("addressPostalCode")]
    public string? AddressPostalCode { get; set; }
}

public record PersonSpecification
{
    public string? Given { get; set; }
    public string? Family { get; set; }
    public DateOnly? BirthDate { get; set; }
    public string? Gender { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? AddressPostalCode { get; set; }
}

public sealed record PersonRecord : PersonSpecification
{
    public string NhsNumber { get; init; } = string.Empty;
}

public sealed record PersonRecordCollection
{
    public List<PersonRecord> People { get; init; } = new();
}
public sealed record PersonMatch(string PersonId);

public sealed record SearchMetadata(
    string PersonId,
    DateTimeOffset RequestedAtUtc
);

public sealed class FetchUrlMappingEntity : ITableEntity
{
    public string PartitionKey { get; set; } = string.Empty;
    public string RowKey { get; set; } = string.Empty;
    public DateTimeOffset? Timestamp { get; set; }
    public ETag ETag { get; set; }

    public string TargetUrl { get; set; } = string.Empty;
    public DateTimeOffset ExpiresAtUtc { get; set; }

    public string TargetOrgId { get; set; } = string.Empty;
    public string RequestingOrgId { get; set; } = string.Empty;
    public string RecordType { get; set; } = string.Empty;

    public string JobId { get; set; } = string.Empty;
}

public sealed record MaskedUrl(string FetchId, string Url, DateTimeOffset ExpiresAtUtc);

public sealed record ResolvedFetchMapping(
    string TargetUrl,
    string TargetOrgId,
    string RecordType
);
