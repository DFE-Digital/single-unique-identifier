using SUI.Find.Domain.Models;
using SUI.Find.Domain.ValueObjects;

namespace SUI.Find.Application.Interfaces.Matching;

public interface IMatchingService
{
    Task<Result<NhsPersonId>> SearchAsync(PersonSpecification personSpecification);
}
