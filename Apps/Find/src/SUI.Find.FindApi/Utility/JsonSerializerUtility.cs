using System.Text.Json;

namespace SUI.Find.FindApi.Utility;

public static class JsonSerializerUtility
{
    public static JsonSerializerOptions GetCaseInsensitiveOptions()
    {
        return new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
    }
}
