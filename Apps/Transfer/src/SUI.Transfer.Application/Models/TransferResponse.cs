using SUI.Transfer.Application.Models.Custodians;

namespace SUI.Transfer.Application.Models;

public class TransferResponse(CustodianResponse custodianResponse)
{
    public ValidationResults? ValidationResults { get; init; } =
        custodianResponse.ValidationResults;
    public ConsolidatedData? ConsolidatedData { get; init; } = custodianResponse.ConsolidatedData;
}
