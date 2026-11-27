using OneOf;
using OneOf.Types;
using SUI.GetAnIdentifier.Application.Models;

namespace SUI.GetAnIdentifier.Application.Interfaces;

public interface IGetAnIdentifierService
{
    Task<OneOf<GetAnIdentifierResult, DataQualityResult, NotFound, Error>> MatchPersonAsync(
        PersonSpecification request,
        string odsCode,
        string? correlationId = null,
        CancellationToken ct = default
    );
}
