using SUI.Find.Domain.Models;

namespace SUI.Find.Application.Interfaces;

public interface IPersonIdEncryptionService
{
    Result<string> EncryptNhsToPersonId(string nhsNumber, EncryptionDefinition encryption);
    Result<string> DecryptPersonIdToNhs(string personId, EncryptionDefinition encryption);
}
