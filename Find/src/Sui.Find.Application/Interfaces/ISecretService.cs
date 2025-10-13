namespace SUI.Find.Application.Interfaces;

public interface ISecretService
{
    Task<string> GetSecret(string secretName, CancellationToken cancellationToken);
}