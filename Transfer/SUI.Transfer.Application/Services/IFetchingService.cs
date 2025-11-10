using SUI.Transfer.Application.Models;

namespace SUI.Transfer.Application.Services;

public interface IFetchingService
{
    Task<FetchResponse> FetchAsync(string id);
}