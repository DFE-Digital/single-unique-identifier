using Microsoft.Extensions.Configuration;

namespace SUI.StubCustodians.Application.Extensions;

public static class ConfigExtensions
{
    public static bool UseEncryptedId(this IConfiguration config)
    {
        var value = !string.IsNullOrWhiteSpace(config["UseEncryptedId"])
            ? config["UseEncryptedId"]
            : "false";
        return bool.Parse(value);
    }
}
