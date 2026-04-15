namespace SUI.Find.Application.Models.Pep;

public sealed record JobContext(
    ProviderDefinition Custodian,
    ProviderDefinition SearchingOrganisation,
    string SearchingOrganisationId
);
