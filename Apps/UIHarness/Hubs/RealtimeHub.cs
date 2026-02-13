using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace UIHarness.Hubs;

[Authorize]
public sealed class RealtimeHub : Hub
{
    public override async Task OnConnectedAsync()
    {
        var org = Context.User?.FindFirst("org")?.Value ?? string.Empty;

        if (!string.IsNullOrWhiteSpace(org))
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, OrgGroup(org));
        }

        await base.OnConnectedAsync();
    }

    public static string OrgGroup(string org) => $"org:{org}";
}
