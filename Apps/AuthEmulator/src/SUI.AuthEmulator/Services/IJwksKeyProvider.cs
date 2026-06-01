using System.Security.Cryptography;
using Microsoft.IdentityModel.Tokens;
using SUI.AuthEmulator.Models;

namespace SUI.AuthEmulator.Services;

public interface IJwksKeyProvider
{
    IReadOnlyCollection<RsaKeyDetails> GetKeys();
}
