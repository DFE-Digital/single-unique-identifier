using OneOf;
using OneOf.Types;
using SUI.Find.Application.Interfaces.Matching;
using SUI.Find.Application.Models.Matching;
using SUI.Find.Domain.ValueObjects;

namespace SUI.Find.Application.Services.Matching;

public class MatchingNhsNumberService : IMatchingNhsNumberService
{
    public Task<OneOf<NhsPersonId, NotFound, Error>> MatchPersonAsync(
        PersonSpecification request,
        string clientId
    )
    {
        throw new NotImplementedException();
    }
}
