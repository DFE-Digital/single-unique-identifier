namespace SUI.Find.E2ETests;

public record Config
{
    public string BaseUrl { get; init; } = "http://localhost:7182/api/";

    public bool IsLocal => BaseUrl.Contains("localhost");

    public bool SkipResetAzureTables { get; init; }

    public string? FindApiStorageConnectionString { get; init; } = "UseDevelopmentStorage=true";

    public bool UseEncryptedIds { get; set; } = true;
}
