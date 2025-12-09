using SUI.Find.Domain.Models;

namespace SUI.Find.Application.Interfaces;

public interface IProviderHttpClient
{
    Task<Result<string>> GetAsync(string url, string? bearerToken, CancellationToken ct);
}