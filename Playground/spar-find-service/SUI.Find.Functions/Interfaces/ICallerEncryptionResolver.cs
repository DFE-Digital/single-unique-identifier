using Models;

namespace Interfaces;

public interface ICallerEncryptionResolver
{
    EncryptionDefinition ResolveForCaller(AuthContext auth);
}