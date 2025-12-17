using System.Collections;
using System.Collections.Frozen;

namespace SUI.Find.Domain.Models;

public class CompiledPolicyArtifact
{
    public string PolicyVersionId { get; init; } = string.Empty;
    public DateTimeOffset CompiledAtUtc { get; init; }

    // used frozen set as its use case is cited as creating read only ids for fast look ups
    // https://learn.microsoft.com/en-us/dotnet/api/system.collections.frozen.frozenset-1?view=net-10.0
    // https://medium.com/c-sharp-programming/frozen-collections-in-net-8-055b007587d0
    public FrozenSet<string> AllowedRequests { get; init; } = FrozenSet<string>.Empty;
}

public record PolicyCheckRequest(
    string SourceOrgId,
    string DestOrgId,
    string DataType,
    string Purpose,
    string Mode
);

public record PolicyDecision(
    bool IsAllowed,
    string Reason,
    string PolicyVersionId
);