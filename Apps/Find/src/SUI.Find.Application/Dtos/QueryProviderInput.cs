using SUI.Find.Application.Models;

namespace SUI.Find.Application.Dtos;

public sealed record QueryProviderInput(
    string RequestingOrg,
    string JobId,
    string InvocationId,
    string Suid,
    ProviderDefinition Provider
);
