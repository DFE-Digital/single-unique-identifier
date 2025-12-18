using SUI.Find.Application.Models;
using SUI.Find.Domain.Models.Policy;

namespace SUI.Find.Infrastructure.Interfaces;

public interface IPolicyCompilerService
{
    CompiledPolicyArtefact Compile(IEnumerable<ProviderDefinition> providers);
}