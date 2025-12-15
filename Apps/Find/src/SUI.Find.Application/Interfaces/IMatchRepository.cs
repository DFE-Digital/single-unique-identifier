using OneOf;
using OneOf.Types;
using SUI.Find.Application.Models;

namespace SUI.Find.Application.Interfaces;

public interface IMatchRepository
{
    Task<OneOf<string, NotFound, Error>> MatchPersonAsync(MatchPersonRequest request);
}
