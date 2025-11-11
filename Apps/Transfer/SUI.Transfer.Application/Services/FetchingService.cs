using SUI.Transfer.Application.Models;

namespace SUI.Transfer.Application.Services;

public class FetchingService : IFetchingService
{
    public Task<FetchResponse> FetchAsync(string id)
    {
        var response = new FetchResponse
        {
            Result = new FetchResult { Id = id },
            Success = true,
        };

        return Task.FromResult(response);
    }
}
