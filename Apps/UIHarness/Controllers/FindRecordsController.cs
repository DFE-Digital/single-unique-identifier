using System.Collections.Concurrent;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using UIHarness.Hubs;
using UIHarness.Interfaces;
using UIHarness.Models;

namespace UIHarness.Controllers;

[Authorize]
public sealed class FindRecordsController(
    ICustodianRepository custodians,
    IFindARecord findARecord,
    IHubContext<RealtimeHub> hub,
    IBackgroundTaskQueue queue) : Controller
{
    private readonly ICustodianRepository _custodians = custodians ?? throw new ArgumentNullException(nameof(custodians));
    private readonly IFindARecord _findARecord = findARecord ?? throw new ArgumentNullException(nameof(findARecord));
    private readonly IHubContext<RealtimeHub> _hub = hub ?? throw new ArgumentNullException(nameof(hub));
    private readonly IBackgroundTaskQueue _queue = queue ?? throw new ArgumentNullException(nameof(queue));

    private static readonly ConcurrentDictionary<string, CancellationTokenSource> _findRecordsCtsByOrg = new(StringComparer.Ordinal);

    [HttpPost("/find-records/cancel")]
    [ValidateAntiForgeryToken]
    public IActionResult Cancel()
    {
        var org = User.FindFirst("org")?.Value ?? string.Empty;
        if (string.IsNullOrWhiteSpace(org))
        {
            return Unauthorized();
        }

        if (_findRecordsCtsByOrg.TryRemove(org, out var cts))
        {
            try
            {
                cts.Cancel();
            }
            finally
            {
                cts.Dispose();
            }
        }

        return Accepted();
    }

    [HttpPost("/find-records")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> FindRecords([FromForm] FindRecordsRequest request, CancellationToken cancellationToken)
    {
        if (request.PersonId == Guid.Empty || string.IsNullOrWhiteSpace(request.NhsNumber))
        {
            return BadRequest();
        }

        var org = User.FindFirst("org")?.Value ?? string.Empty;
        if (string.IsNullOrWhiteSpace(org))
        {
            return Unauthorized();
        }

        if (_findRecordsCtsByOrg.TryRemove(org, out var existing))
        {
            try
            {
                existing.Cancel();
            }
            finally
            {
                existing.Dispose();
            }
        }

        var cts = new CancellationTokenSource();
        _findRecordsCtsByOrg[org] = cts;
        var searchToken = cts.Token;
        var searchId = Guid.NewGuid();

        var directory = await _custodians.GetAllAsync(cancellationToken);

        await _hub.Clients.Group(RealtimeHub.OrgGroup(org)).SendAsync(
            "FindRecordsStarted",
            new FindRecordsStarted
            {
                SearchId = searchId,
                NhsNumber = request.NhsNumber,
                TotalCustodians = directory.Count
            },
            cancellationToken);

        foreach (var custodian in directory)
        {
            var custodianId = custodian.CustodianId;
            var custodianName = custodian.Name;
            var nhsNumber = request.NhsNumber;

            await _queue.QueueAsync(async ct =>
            {
                if (searchToken.IsCancellationRequested)
                {
                    return;
                }

                var recordTypes = await _findARecord.FindRecordTypesAsync(custodian, nhsNumber, searchToken);

                if (searchToken.IsCancellationRequested)
                {
                    return;
                }

                await _hub.Clients.Group(RealtimeHub.OrgGroup(org)).SendAsync(
                    "CustodianSearchCompleted",
                    new CustodianSearchCompleted
                    {
                        SearchId = searchId,
                        CustodianId = custodianId,
                        HasMatch = recordTypes.Count > 0
                    },
                    ct);

                foreach (var recordType in recordTypes)
                {
                    if (searchToken.IsCancellationRequested)
                    {
                        return;
                    }

                    var recordUrl =
                        $"/record/{Uri.EscapeDataString(custodianId)}/{Uri.EscapeDataString(nhsNumber)}/{Uri.EscapeDataString(recordType)}";

                    await _hub.Clients.Group(RealtimeHub.OrgGroup(org)).SendAsync(
                        "FindRecordRow",
                        new FindRecordRow
                        {
                            SearchId = searchId,
                            CustodianName = custodianName,
                            RecordType = recordType,
                            RecordUrl = recordUrl
                        },
                        ct);
                }
            }, cancellationToken);
        }

        return Accepted();
    }
}
