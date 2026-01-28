using OneOf;
using OneOf.Types;
using SUI.Find.Application.Models.Matching;
using SUI.Find.Domain.ValueObjects;

namespace SUI.Find.Application.Interfaces.Matching;

public interface IMatchingNhsNumberService
{
    Task<OneOf<NhsPersonId, DataQualityResult, NotFound, Error>> MatchPersonAsync(
        PersonSpecification request,
        CancellationToken ct
    );
}
