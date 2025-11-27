using System.Net;
using SUI.Transfer.Application.Models.Custodians;

namespace SUI.Transfer.Application.Services;

public class CustodianService(HttpClient httpClient) : ICustodianService
{
    private readonly IEnumerable<Custodian> _custodians = new List<Custodian>
    {
        new()
        {
            RecordLocation = "/Arbor/api/A3524E91-C15C-47F8-8AA8-886E0957ADC9",
            RecordType = RecordType.Education,
        },
    }; // TODO - hardcoded list of custodians

    public async Task<CustodianResponse> GetConsolidatedDataFromSui(string id)
    {
        var data = new List<ICustodianRecord>();
        var errors = new List<(HttpStatusCode StatusCode, string? ReasonPhrase)>();

        foreach (var custodian in _custodians)
        {
            var custodianResponse = await httpClient.GetAsync(custodian.RecordLocation);

            if (!custodianResponse.IsSuccessStatusCode)
                errors.Add((custodianResponse.StatusCode, custodianResponse.ReasonPhrase));
            else
                data.Add(ProcessContent(custodianResponse, custodian.RecordType));
        }

        return Consolidate(data, errors);
    }

    private CustodianResponse Consolidate(
        List<ICustodianRecord> data,
        List<(HttpStatusCode StatusCode, string? ReasonPhrase)> errors
    )
    {
        // Full implementation out of scope for SUI-1009
        // TODO - Implement basic version - should use first object of each record type in entirety
        return new CustodianResponse();
    }

    private ICustodianRecord ProcessContent(
        HttpResponseMessage custodianResponse,
        RecordType custodianRecordType
    )
    {
        // TODO - Return API result in entity form, based on record type
        return new EducationData();
    }
}
