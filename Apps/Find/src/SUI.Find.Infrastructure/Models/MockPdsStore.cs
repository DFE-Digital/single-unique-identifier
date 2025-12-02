namespace SUI.Find.Infrastructure.Models;

public class MockPdsStore
{
    public List<Person> People { get; set; } = new();
}

public class Person
{
    public string NhsNumber { get; set; } = null!;
    public string Given { get; set; } = null!;
    public string Family { get; set; } = null!;
    public DateOnly BirthDate { get; set; }
    public string Gender { get; set; } = null!;
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string AddressPostalCode { get; set; } = null!;
}
