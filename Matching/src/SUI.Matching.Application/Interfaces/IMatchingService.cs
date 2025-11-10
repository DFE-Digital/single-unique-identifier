using SUI.Matching.Application.Models;

namespace SUI.Matching.Application.Interfaces;

public interface IMatchingService
{
    Task<PersonMatchResponse> SearchAsync(PersonSpecification personSpecification);
}
