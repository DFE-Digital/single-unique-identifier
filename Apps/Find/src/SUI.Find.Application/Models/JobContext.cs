namespace SUI.Find.Application.Models;

public sealed record JobContext(
    ProviderDefinition Custodian,
    ProviderDefinition SearchingOrganisation,
    string SearchingOrganisationId
);
