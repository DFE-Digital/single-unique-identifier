using SUI.Find.Application.Models;

namespace SUI.Find.Application.Interfaces;

public interface IFetchUrlStorageService
{
    Task AddAsync(AddFetchUrlRequest request, CancellationToken ct);
}
