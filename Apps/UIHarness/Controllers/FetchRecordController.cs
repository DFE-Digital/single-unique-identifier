using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UIHarness.Interfaces;

namespace UIHarness.Controllers;

[Authorize]
public sealed class FetchRecordController(
    ICustodianRepository custodians,
    IFetchARecord fetchARecord) : Controller
{
    private readonly ICustodianRepository _custodians = custodians ?? throw new ArgumentNullException(nameof(custodians));
    private readonly IFetchARecord _fetchARecord = fetchARecord ?? throw new ArgumentNullException(nameof(fetchARecord));

    [HttpGet("/record/{custodianId}/{nhsNumber}/{recordType}")]
    public async Task<IActionResult> Get(
        [FromRoute] string custodianId,
        [FromRoute] string nhsNumber,
        [FromRoute] string recordType,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(custodianId) || string.IsNullOrWhiteSpace(nhsNumber) || string.IsNullOrWhiteSpace(recordType))
        {
            return BadRequest();
        }

        var custodian = await _custodians.GetByIdAsync(custodianId, cancellationToken);
        if (custodian is null)
        {
            return NotFound();
        }

        var decodedType = Uri.UnescapeDataString(recordType);
        var response = await _fetchARecord.FetchAsync(custodian, nhsNumber, decodedType, cancellationToken);

        return Ok(response);
    }
}