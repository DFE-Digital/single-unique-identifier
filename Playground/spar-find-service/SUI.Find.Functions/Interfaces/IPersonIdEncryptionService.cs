using Models;

namespace Interfaces;

public interface IPersonIdEncryptionService
{
    string EncryptNhsToPersonId(string nhsNumber, EncryptionDefinition encryption);
    string DecryptPersonIdToNhs(string personId, EncryptionDefinition encryption);
}
