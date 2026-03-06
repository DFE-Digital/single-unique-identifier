namespace SUI.Find.FindApi.Models;

public record Problem(string Type, string Title, int Status, string? Detail, string? Instance);
