using SUI.Find.Domain.Models.Policy;

namespace SUI.Find.Application.Interfaces;

public interface IPolicyEnforcementService
{
    Task<PolicyDecision> EvaluateDsaAsync(PolicyCheckRequest request);

}