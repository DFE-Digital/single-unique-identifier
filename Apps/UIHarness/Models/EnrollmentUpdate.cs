namespace UIHarness.Models;

public sealed class EnrollmentUpdate
{
    public Guid PersonId { get; set; }
    public string NhsNumber { get; set; } = string.Empty;
}
