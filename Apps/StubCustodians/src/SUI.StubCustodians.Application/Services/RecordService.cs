using System.Text.Json;
using SUI.StubCustodians.Application.Interfaces;
using SUI.StubCustodians.Application.Models;

namespace SUI.StubCustodians.Application.Services;

public class RecordService(IDataProvider dataProvider) : IRecordService
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
        var personRecord = orgRecord.Payload.Deserialize<T>(JsonSerializerOptions.Web);
        if (personRecord == null)
        {
            return null;
        }

        var result = new RecordEnvelope<T>
        {
            SchemaUri = new Uri(orgRecord.SchemaUri),
            Payload = personRecord,
        };
        return result;
    }
}
