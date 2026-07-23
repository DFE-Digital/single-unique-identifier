using OneOf;
using OneOf.Types;
using SUI.GetAnIdentifier.Application.Models;
using SUI.GetAnIdentifier.Domain.Models;

namespace SUI.GetAnIdentifier.Application.Interfaces;

public interface IGetAnIdentifierService
{
    Task<OneOf<NhsPersonId, DataQualityResult, NotFound, Error>> MatchPersonAsync(
        PersonSpecification request,
        string authContextOrganisationId,
        CancellationToken ct
    );
}
