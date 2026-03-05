namespace SUI.UIHarness.Web.Models.Find;

public class FindMatchPerson
{
    public string? Given { get; set; }
    public string? Family { get; set; }
    public DateOnly? BirthDate { get; set; }
    public string? Gender { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? AddressPostalCode { get; set; }
}
