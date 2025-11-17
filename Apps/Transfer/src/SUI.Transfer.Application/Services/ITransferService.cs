using SUI.Transfer.Application.Models;

namespace SUI.Transfer.Application.Services;

public interface ITransferService
{
    Task<TransferResponse> TransferAsync(string id);
}
