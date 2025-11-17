namespace SUI.Transfer.Application.Models;

public class TransferResponse
{
    public required bool Success { get; init; }
    public required TransferResult Result { get; init; }
}
