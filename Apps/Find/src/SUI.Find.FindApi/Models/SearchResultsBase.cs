using System.Text.Json.Serialization;
using SUI.Find.Application.Enums;
using SUI.Find.Application.Models;

namespace SUI.Find.FindApi.Models;

public record SearchResultsBase
{
    public required string Suid { get; init; } = string.Empty;

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public required SearchStatus Status { get; init; }

    public SearchResultItem[] Items { get; init; } = [];
}
