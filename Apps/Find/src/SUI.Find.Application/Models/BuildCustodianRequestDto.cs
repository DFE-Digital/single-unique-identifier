namespace SUI.Find.Application.Models;

public record BuildCustodianRequestDto
(
    ProviderDefinition Provider,
    string EncryptedPersonId,
    string? AccessToken
);