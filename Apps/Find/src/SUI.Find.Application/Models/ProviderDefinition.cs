using SUI.Find.Domain.Models;

namespace SUI.Find.Application.Models;

public sealed class ProviderDefinition
{
    public string OrgId { get; init; } = string.Empty;
    public string OrgName { get; init; } = string.Empty;
    public string OrgType { get; init; } = string.Empty;

    public string ProviderSystem { get; init; } = string.Empty;
    public string ProviderName { get; init; } = string.Empty;

    public string RecordType { get; init; } = string.Empty;

    public ConnectionDefinition Connection { get; init; } = new();
    public EncryptionDefinition? Encryption { get; init; }

    public DsaPolicyDefinition DsaPolicy { get; set; } = new();
}

public sealed class ConnectionDefinition
{
    public string Method { get; init; } = "GET";
    public string Url { get; init; } = string.Empty;

    public string PersonIdPosition { get; init; } = "path";

    public RecordTypeFilterDefinition? RecordTypeFilter { get; init; }

    public AuthDefinition? Auth { get; init; }

    public ResponseDefinition? Response { get; init; }

    public string? BodyTemplateJson { get; init; }
}

public sealed class RecordTypeFilterDefinition
{
    public bool Supported { get; init; }
    public string Position { get; init; } = "query";
    public string Name { get; init; } = "recordType";
}

public sealed class AuthDefinition
{
    public string Type { get; init; } = "oauth2_client_credentials";

    public string TokenUrl { get; init; } = string.Empty;

    // *** THESE ARE THE REQUESTED SCOPES ***
    public IReadOnlyList<string> Scopes { get; init; } = Array.Empty<string>();

    // *** THESE ARE THE CLIENT CREDENTIALS FOR THAT CUSTODIAN ***
    public string ClientId { get; init; } = string.Empty;
    public string ClientSecret { get; init; } = string.Empty;
}

public sealed class DsaPolicyDefinition
{
    public DateTimeOffset Version { get; init; }
    public List<DsaRuleDefinition> Defaults { get; init; } = [];
    public List<DsaRuleDefinition> Exceptions { get; init; } = [];
}

public sealed class DsaRuleDefinition
{
    public string Effect { get; init; } = string.Empty;

    public List<string> Modes { get; init; } = [];

    public List<string> DataTypes { get; init; } = [];

    public List<string> DestOrgTypes { get; init; } = [];

    public List<string> Purposes { get; init; } = [];

    public List<string> DestOrgIds { get; init; } = [];

    public DateTimeOffset? ValidFrom { get; init; }

    public DateTimeOffset? ValidUntil { get; init; }

    public string? Reason { get; init; }
}

public sealed class ResponseDefinition
{
    public string Shape { get; init; } = "searchResultItems";
}
