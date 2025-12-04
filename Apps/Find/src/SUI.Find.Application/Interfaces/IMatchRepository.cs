using SUI.Find.Application.Models;

namespace SUI.Find.Application.Interfaces;

public interface IMatchRepository
{
    Task<MatchFhirResponse> MatchPersonAsync(MatchPersonRequest request);
}
