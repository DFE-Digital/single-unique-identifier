namespace SUI.FakeCustodians.Application.Models
{
    public record EducationData
    {
        public bool PupilPremium { get; init; }
    
        public bool FreeSchoolMeals { get; init; }
    
        public bool ElectivelyHomeEducated { get; init; }
    
        public IEnumerable<School>? SchoolsAttended { get; init; }
    }
}