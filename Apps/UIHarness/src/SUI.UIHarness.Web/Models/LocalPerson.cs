namespace SUI.UIHarness.Web.Models;

public record LocalPerson(
    Guid PersonId,
    string Given,
    string Family,
    DateOnly BirthDate,
    string Gender,
    string? Email,
    string? Phone,
    string Postcode,
    string? NhsNumber
)
{
    public string? NhsNumber { get; set; } = NhsNumber;
}
