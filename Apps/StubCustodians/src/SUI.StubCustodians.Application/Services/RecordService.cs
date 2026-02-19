using System.Text.Json;
using Microsoft.Extensions.Configuration;
using SUI.StubCustodians.Application.Extensions;
using SUI.StubCustodians.Application.Interfaces;
using SUI.StubCustodians.Application.Models;

namespace SUI.StubCustodians.Application.Services;

public class RecordService(IDataProvider dataProvider, IConfiguration configuration)
    : IRecordService
{
    public async Task<RecordEnvelope<T>?> GetRecord<T>(string recordId, string orgId)
        where T : class
    {
        var orgRecord = await dataProvider.GetRecordByIdAsync(
            orgId,
            recordId,
            CancellationToken.None
        );
        if (orgRecord == null)
        {
            return null;
        }
        var personRecord = orgRecord.Payload?.Deserialize<T>(JsonSerializerOptions.Web);
        if (personRecord == null)
        {
            return null;
        }

        var useEncryptedId = configuration.UseEncryptedId();
        var result = new RecordEnvelope<T>
        {
            PersonId = useEncryptedId ? orgRecord.EncryptedPersonId : orgRecord.PersonId,
            RecordId = orgRecord.RecordId,
            RecordType = orgRecord.RecordType,
            Version = orgRecord.Version,
            SchemaUri = new Uri(orgRecord.SchemaUri),
            ContactDetails = orgRecord.ContactDetails,
            RecordLink = orgRecord.RecordLink,
            Payload = personRecord,
        };
        return result;
    }
}
