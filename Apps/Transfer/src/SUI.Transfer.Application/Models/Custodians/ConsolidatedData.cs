namespace SUI.Transfer.Application.Models.Custodians;

public record ConsolidatedData
{
    public required string Sui { get; init; }

    public PersonalData? PersonalData { get; init; }

    public EducationData? EducationData { get; init; }

    public PoliceData? PoliceData { get; init; }

    public ProbationData? ProbationData { get; init; }

    public GpData? GpData { get; init; }

    public CamhsData? CamhsData { get; init; }
}
