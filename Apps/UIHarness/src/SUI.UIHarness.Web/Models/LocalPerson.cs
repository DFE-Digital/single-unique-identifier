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
    string? NhsNumber,
    IReadOnlyCollection<string>? GpOdsCodes
)
{
    public string? NhsNumber { get; set; } = NhsNumber;

    public IReadOnlyCollection<string>? GpOdsCodes { get; set; } = GpOdsCodes;
}
