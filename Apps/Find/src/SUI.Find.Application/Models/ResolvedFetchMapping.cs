namespace SUI.Find.Application.Models;

public sealed record ResolvedFetchMapping(string TargetUrl, string TargetOrgId, string RecordType);

public abstract record ResolvedFetchMappingResult
{
    public sealed record Success(ResolvedFetchMapping ResolvedFetchMapping)
        : ResolvedFetchMappingResult;

    public sealed record Unauthorized() : ResolvedFetchMappingResult;

    public sealed record Expired() : ResolvedFetchMappingResult;

    public sealed record NotFound() : ResolvedFetchMappingResult;

    public sealed record Fail() : ResolvedFetchMappingResult;
}
