namespace SUI.StubCustodians.Application.Interfaces;

public interface ITokenProvider
{
    Task<string> GetTokenAsync(string clientId, string clientSecret);
}
