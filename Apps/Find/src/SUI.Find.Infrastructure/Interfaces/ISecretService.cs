namespace SUI.Find.Infrastructure.Interfaces;

public interface ISecretService
{
    Task<string> GetSecretAsync(string secretName, CancellationToken cancellationToken);
}
