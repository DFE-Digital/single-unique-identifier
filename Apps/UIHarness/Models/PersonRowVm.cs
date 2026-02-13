namespace UIHarness.Models;

public sealed class PersonRowVm
{
    public Guid PersonId { get; set; }
    public string Given { get; set; } = string.Empty;
    public string Family { get; set; } = string.Empty;
    public DateOnly Birthdate { get; set; }
    public string Gender { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string Postcode { get; set; } = string.Empty;
    public string? NhsNumber { get; set; }
}
