using SUI.Find.Domain.Models;

namespace SUI.Find.Application.Models;

public interface IPolicyEnforcementPoint
{
    Task<PolicyDecision> EvaluateDsaAsync(PolicyCheckRequest request);

}