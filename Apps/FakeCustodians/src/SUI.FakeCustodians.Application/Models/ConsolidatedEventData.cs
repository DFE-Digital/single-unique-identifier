namespace SUI.FakeCustodians.Application.Models
{
    public record ConsolidatedEventData
    {
        public PersonalData? PersonalData { get; init; }
    
        public EducationData? EducationData { get; init; }
    
        public PoliceData? PoliceData { get; init; }
    
        public ProbationData? ProbationData { get; init; }
    
        public GpData? GpData { get; init; }
    
        public CamhsData? CamhsData { get; init; }
    }
}