using SUi.Find.Application.Models;

namespace SUi.Find.Application.Interfaces;

public interface IMatchingService
{
    Task<PersonMatchResponse> SearchAsync(PersonSpecification personSpecification);
}