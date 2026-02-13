namespace UIHarness.Models;

public sealed class ContactRecord
{
    public string Name { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public string? Telephone { get; set; }
    public string? Email { get; set; }
    public string? Address { get; set; }
}
