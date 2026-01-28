using OneOf;
using OneOf.Types;
using SUI.Find.Application.Models.Matching;
using SUI.Find.Application.Services;

namespace SUI.Find.Application.Interfaces;

public interface IPersonIdRepresentationService
{
    // FindPersonIdAsync
    Task<OneOf<PersonIdValue, DataQualityResult, NotFound, Error>> FindPersonIdAsync(
        PersonSpecification specification,
        string clientId
    );
}
