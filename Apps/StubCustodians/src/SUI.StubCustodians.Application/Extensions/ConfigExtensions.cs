using Microsoft.Extensions.Configuration;

namespace SUI.StubCustodians.Application.Extensions;

public static class ConfigExtensions
{
    public static bool UseEncryptedId(this IConfiguration config)
    {
        return bool.Parse(config["UseEncryptedId"] ?? "false");
    }
}
