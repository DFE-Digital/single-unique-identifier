using SUI.Find.Application.Models;

namespace SUI.Find.Application.Interfaces;

public interface IMatchingService
{
    Task<PersonMatchResponse> SearchAsync(PersonSpecification personSpecification);
}