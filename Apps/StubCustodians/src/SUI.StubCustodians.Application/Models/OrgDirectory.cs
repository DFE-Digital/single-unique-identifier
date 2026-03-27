namespace SUI.StubCustodians.Application.Models;

public class OrgDirectory
{
    public List<Organisation> Organisations { get; set; } = [];
}

public class Organisation
{
    public required string OrgId { get; set; }
    public List<RecordDefinition> Records { get; set; } = [];
}

public class RecordDefinition
{
    public required string RecordType { get; set; }
    public required Connection Connection { get; set; }
}

public class Connection
{
    public required AuthConfig Auth { get; set; }
}

public class AuthConfig
{
    public required string ClientId { get; set; }
    public required string ClientSecret { get; set; }
}
