namespace SUI.SingleView.Application.Models;

internal readonly record struct SearchQuery(
    string? NhsNumber,
    string? FirstName,
    string? LastName,
    DateTime? DateOfBirth,
    string? HouseNumberOrName,
    string? Postcode
);
