using SUI.Find.Application.Models;

namespace SUI.Find.Application.Dtos;

public sealed record QueryProviderInput(string ClientId, string InstanceId, string InvocationId, string Suid, ProviderDefinition Provider);
