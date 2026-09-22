using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Options;
using SUI.NotificationService.Application.Interfaces;
using SUI.NotificationService.Application.Models;
using SUI.NotificationService.Mesh.Configuration;

namespace SUI.NotificationService.Mesh;

/// <summary>
/// Reads messages from an NHS MESH mailbox over the MESH REST API.
/// </summary>
internal sealed class MeshInboxClient(HttpClient httpClient, IOptions<NhsMeshConfig> meshConfig)
    : IMeshMessageReceiver
{
    public const string HttpClientName = "nhs-mesh-api";

    private readonly string _mailboxId = meshConfig.Value.MailboxId;

    public async Task<int> GetMessageCountAsync(CancellationToken cancellationToken = default)
    {
        // Use the dedicated count endpoint rather than /inbox: the inbox listing is paginated by
        // MESH, so its message array only reflects a single page and would undercount once a
        // mailbox has more messages than fit on one page.
        var response = await httpClient.GetAsync(
            $"/messageexchange/{_mailboxId}/count", // TODO: this might be deprecated, double check.
            cancellationToken
        );
        response.EnsureSuccessStatusCode();

        var count = await response.Content.ReadFromJsonAsync<MeshInboxCountResponse>(
            cancellationToken
        );
        return count?.Count ?? 0;
    }

    public async Task<IReadOnlyList<string>> GetMessageIdsAsync(
        CancellationToken cancellationToken = default
    )
    {
        var response = await httpClient.GetAsync(
            $"/messageexchange/{_mailboxId}/inbox",
            cancellationToken
        );
        response.EnsureSuccessStatusCode();

        var inbox = await response.Content.ReadFromJsonAsync<MeshInboxResponse>(cancellationToken);
        return inbox?.Messages ?? [];
    }

    public async Task<MeshMailboxMessage> ReadMessageAsync(
        string messageId,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(messageId);

        var response = await httpClient.GetAsync(
            $"/messageexchange/{_mailboxId}/inbox/{messageId}",
            cancellationToken
        );
        response.EnsureSuccessStatusCode();

        var content = await response.Content.ReadAsStringAsync(cancellationToken);
        return new MeshMailboxMessage(messageId, content);
    }

    public async Task AcknowledgeMessageAsync(
        string messageId,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(messageId);

        // Acknowledging is how a MESH client confirms it has durably received and processed a
        // message. MESH then removes the message from the inbox so it won't be redelivered -
        // simply GETting a message does not clear it from the mailbox.
        using var request = new HttpRequestMessage(
            HttpMethod.Put,
            $"/messageexchange/{_mailboxId}/inbox/{messageId}/status/acknowledged"
        );

        var response = await httpClient.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();
    }

    private sealed record MeshInboxCountResponse([property: JsonPropertyName("count")] int Count);

    private sealed record MeshInboxResponse(string[] Messages);
}
