using SUI.Transfer.Application.Models;

namespace SUI.Transfer.Application.Services;

public class TransferService : ITransferService
{
    public Task<TransferResponse> TransferAsync(string id)
    {
        var response = new TransferResponse
        {
            Result = new TransferResult { Id = id },
            Success = true,
        };

        return Task.FromResult(response);
    }
}
