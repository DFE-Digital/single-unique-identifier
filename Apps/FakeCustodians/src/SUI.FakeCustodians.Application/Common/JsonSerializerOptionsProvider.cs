using System.Text.Json;

namespace SUI.FakeCustodians.Application.Common;

public class JsonSerializerOptionsProvider
{
    public static readonly JsonSerializerOptions _options = new()
    {
        PropertyNameCaseInsensitive = true,
    };
}
