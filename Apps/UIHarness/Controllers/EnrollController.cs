using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using UIHarness.Hubs;
using UIHarness.Interfaces;
using UIHarness.Models;

namespace UIHarness.Controllers;

[Authorize]
public sealed class EnrollmentController(
    IPersonRepository repo,
    IFindAnId findAnId,
    IHubContext<RealtimeHub> hub,
    IBackgroundTaskQueue queue) : Controller
{
    private readonly IPersonRepository _repo = repo ?? throw new ArgumentNullException(nameof(repo));
    private readonly IFindAnId _findAnId = findAnId ?? throw new ArgumentNullException(nameof(findAnId));
    private readonly IHubContext<RealtimeHub> _hub = hub ?? throw new ArgumentNullException(nameof(hub));
    private readonly IBackgroundTaskQueue _queue = queue ?? throw new ArgumentNullException(nameof(queue));

    [HttpPost("/enrol")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Enrol(CancellationToken cancellationToken)
    {
        var org = User.FindFirst("org")?.Value ?? string.Empty;

        var people = await _repo.GetAllAsync(cancellationToken);

        foreach (var person in people)
        {
            await _queue.QueueAsync(async ct =>
            {
                var nhs = await _findAnId.EnrolAsync(person, ct);

                var update = new EnrollmentUpdate
                {
                    PersonId = person.PersonId,
                    NhsNumber = nhs
                };

                await _hub.Clients.Group(RealtimeHub.OrgGroup(org)).SendAsync("EnrollmentUpdate", update, ct);
            }, cancellationToken);
        }

        return Accepted();
    }
}
