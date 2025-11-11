namespace SUI.Transfer.Application.Models;

public class FetchResponse
{
    public required bool Success { get; init; }
    public required FetchResult Result { get; init; }
}