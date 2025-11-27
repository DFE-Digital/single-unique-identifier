using SUI.Transfer.Application.Models.Custodians;

namespace SUI.Transfer.Application.Services;

public interface ICustodianService
{
    Task<CustodianResponse> GetConsolidatedDataFromSui(string id);
}
