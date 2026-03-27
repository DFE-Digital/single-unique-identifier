using System.Text.Json.Serialization;
using SUI.Find.Application.Dtos;
using SUI.Find.Application.Enums;
using SUI.Find.Application.Models;

namespace SUI.Find.FindApi.Models;

public record SearchResultsBase
{
    public required string Suid { get; set; } = string.Empty;

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public required SearchStatus Status { get; set; }
    public SearchResultItem[] Items { get; init; } = [];
}
