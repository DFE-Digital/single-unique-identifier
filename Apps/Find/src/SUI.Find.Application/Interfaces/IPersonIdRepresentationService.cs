using OneOf;
using OneOf.Types;
using SUI.Find.Application.Models.Matching;
using SUI.Find.Application.Services;

namespace SUI.Find.Application.Interfaces;

/// <summary>
///  Orchestrates the process of matching a person and returning their Person ID representation,
///  either encrypted or plain based on client configuration and global settings.
/// </summary>
public interface IPersonIdRepresentationService
{
    Task<OneOf<PersonIdValue, DataQualityResult, NotFound, Error>> FindPersonIdAsync(
        PersonSpecification specification,
        string clientId
    );
}
