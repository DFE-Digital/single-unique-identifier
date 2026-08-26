namespace SUI.GetAnIdentifier.Application.Models;

public record GetAnIdentifierResult(
    NhsPersonId PersonId,
    IReadOnlyCollection<string> GeneralPractitioner
);
