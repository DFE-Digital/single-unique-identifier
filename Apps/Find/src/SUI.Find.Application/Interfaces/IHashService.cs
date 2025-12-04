namespace SUI.Find.Application.Interfaces;

public interface IHashService
{
    string HmacSha256Hash(string input);
}
