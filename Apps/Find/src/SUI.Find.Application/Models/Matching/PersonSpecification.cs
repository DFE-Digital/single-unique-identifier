using System.ComponentModel.DataAnnotations;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Attributes;

namespace SUI.Find.Application.Models.Matching;

public class PersonSpecification
{
    [OpenApiProperty(
        Description = "Given names (not always 'first'). Includes middle names",
        Default = "Octavia"
    )]
    public string? Given { get; set; }

    [OpenApiProperty(Description = "Family name (often called 'Surname')", Default = "Chislett")]
    public string? Family { get; set; }

    // Type and Format is handled by DataType attribute
    [OpenApiProperty(Description = "The date of birth for the individual", Default = "2022-03-17")]
    [DataType(DataType.Date)]
    public DateOnly? BirthDate { get; set; }

    [OpenApiProperty(
        Description = "The gender that the patient is considered to have for administration and record keeping purposes."
    )]
    public string? Gender { get; set; }

    [OpenApiProperty(Description = "A telephone number by which the individual may be contacted.")]
    public string? Phone { get; set; }

    [OpenApiProperty(Description = "An email address by which the individual may be contacted.")]
    public string? Email { get; set; }

    [OpenApiProperty(
        Description = "Postal code for address of the individual.",
        Default = "KT19 0ST"
    )]
    public string? AddressPostalCode { get; set; }
}
