namespace SUI.Find.Application.Models.Pep;

public sealed record PepContext(
    ProviderDefinition Custodian,
    ProviderDefinition SearchingOrganisation,
    string SearchingOrganisationId
);
