using OneOf;
using OneOf.Types;
using SUI.Find.Application.Models;

namespace SUI.Find.Application.Interfaces;

public interface IFetchRecordService
{
    Task<OneOf<CustodianRecord, NotFound, Unauthorized, Error>> FetchRecordAsync(
        string fetchId,
        string requestingOrgId,
        CancellationToken cancellationToken
    );
}
