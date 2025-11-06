using FluentValidation;

namespace SUI.Find.Application.Models;

public class PersonSpecification
{
    public string? Given { get; set; }
    public string? Family { get; set; }
    public DateOnly? BirthDate { get; set; }
    public string? Gender { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? AddressPostalCode { get; set; }
}

