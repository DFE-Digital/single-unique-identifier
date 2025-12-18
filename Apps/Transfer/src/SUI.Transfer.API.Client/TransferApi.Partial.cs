using System.Text.Json.Serialization;

namespace SUI.Transfer.API.Client;

public partial class TransferApi
{
    static partial void UpdateJsonSerializerSettings(
        System.Text.Json.JsonSerializerOptions settings
    )
    {
        settings.Converters.Add(new JsonStringEnumConverter());
    }
}
