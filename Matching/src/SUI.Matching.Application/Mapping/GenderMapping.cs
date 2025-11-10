using SUI.Matching.Domain.ValueObjects;

namespace SUI.Matching.Application.Mapping;

public static class GenderMapper
{
    public static Gender FromDbsGenderCode(string? code) =>
        code?.Trim() switch
        {
            "0" => Gender.NotKnown,
            "1" => Gender.Male,
            "2" => Gender.Female,
            "9" => Gender.NotSpecified,
            _ => Gender.Unknown,
        };
}
