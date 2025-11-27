using SUI.Transfer.Application.Models;

namespace SUI.Transfer.Application.Services;

public class TransferService(ICustodianService custodianService, IRepository repository)
    : ITransferService
{
    public async Task<TransferResponse> TransferAsync(string id)
    {
        var custodianResponse = await custodianService.GetConsolidatedDataFromSui(id);

        if (custodianResponse.ConsolidatedData is not null)
        {
            repository.AddOrUpdate(custodianResponse.ConsolidatedData);
        }

        // Errors are passed to API result
        return new TransferResponse(custodianResponse);
    }
}
