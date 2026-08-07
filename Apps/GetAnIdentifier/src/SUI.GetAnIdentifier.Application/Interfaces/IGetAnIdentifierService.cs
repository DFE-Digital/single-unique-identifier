using OneOf;
using OneOf.Types;
using SUI.GetAnIdentifier.Application.Models;

namespace SUI.GetAnIdentifier.Application.Interfaces;

public interface IGetAnIdentifierService
{
    Task<
        OneOf<(NhsPersonId, IEnumerable<string?>), DataQualityResult, NotFound, Error>
    > MatchPersonAsync(PersonSpecification request, CancellationToken ct);
}
