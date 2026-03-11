using SUI.Find.Application.Dtos;

namespace SUI.Find.Application.Interfaces;

public interface IJobClaimService
{
    Task<JobInfo?> ClaimNextAvailableJobAsync(string custodianId);
}
