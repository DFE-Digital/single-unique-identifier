using SUI.Find.Application.Models;
using SUI.Find.Domain.Models;

namespace SUI.Find.Application.Interfaces;

public interface IFetchRecordService
{
    Task<Result<CustodianRecord>> FetchRecordAsync(
        string fetchId,
        string requestingOrgId,
        CancellationToken cancellationToken
    );
}
