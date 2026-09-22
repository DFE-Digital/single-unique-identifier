using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.Extensions.Options;
using SUI.NotificationService.Application.Interfaces;
using SUI.NotificationService.Application.Models;
using SUI.NotificationService.Mesh.Configuration;

namespace SUI.NotificationService.Mesh;

/// <summary>
/// Reads messages from an NHS MESH mailbox over the MESH REST API.
/// </summary>
internal sealed class MeshInboxClient(HttpClient httpClient, IOptions<NhsMeshConfig> meshConfig)
    : IMeshInboxClient
{
    public const string HttpClientName = "nhs-mesh-api";

    // Version 2 of the inbox representation adds "links.next", the cursor MESH wants clients to
    // follow to page through an inbox holding more messages than fit in one response. Without this
    // Accept header MESH returns the version 1 shape, which is a single unpaged page of ids.
    private const string MeshV2MediaType = "application/vnd.mesh.v2+json";

    private readonly string _mailboxId = meshConfig.Value.MailboxId;

    public async Task<IReadOnlyList<string>> GetMessageIdsAsync(
        CancellationToken cancellationToken = default
    )
    {
        // MESH caps each inbox response at 500 message ids, so a single request only describes part
        // of a busy mailbox. The dedicated /count endpoint that used to report the true total is
        // deprecated - MESH's documented poll cycle is to read the inbox and keep following
        // "links.next" until no further page is offered.
        var messageIds = new List<string>();
        var requestUri = $"/messageexchange/{_mailboxId}/inbox";

        while (requestUri is not null)
        {
            var inbox = await GetInboxPageAsync(requestUri, cancellationToken);
            if (inbox?.Messages is not { Length: > 0 })
            {
                break;
            }

            messageIds.AddRange(inbox.Messages);
            requestUri = ResolveNextPageUri(inbox.Links?.Next);
        }

        return messageIds;
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

    private async Task<MeshInboxResponse?> GetInboxPageAsync(
        string requestUri,
        CancellationToken cancellationToken
    )
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, requestUri);
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue(MeshV2MediaType));

        var response = await httpClient.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<MeshInboxResponse>(cancellationToken);
    }

    /// <summary>
    /// Turns the "links.next" value MESH returns into the next request URI, or null when the inbox
    /// has no further pages. MESH documents this as a relative link; anything pointing at another
    /// host is rejected rather than followed, so a bad link can never redirect mailbox credentials
    /// somewhere else.
    /// </summary>
    private string? ResolveNextPageUri(string? next)
    {
        if (string.IsNullOrWhiteSpace(next))
        {
            return null;
        }

        if (!Uri.TryCreate(next, UriKind.RelativeOrAbsolute, out var nextUri))
        {
            throw new InvalidOperationException(
                $"MESH returned an inbox paging link that is not a valid URI: '{next}'."
            );
        }

        if (!nextUri.IsAbsoluteUri)
        {
            return next;
        }

        var isSameHost =
            httpClient.BaseAddress is not null
            && Uri.Compare(
                nextUri,
                httpClient.BaseAddress,
                UriComponents.SchemeAndServer,
                UriFormat.UriEscaped,
                StringComparison.OrdinalIgnoreCase
            ) == 0;

        if (!isSameHost)
        {
            throw new InvalidOperationException(
                "MESH returned an inbox paging link pointing at a different host; refusing to follow it."
            );
        }

        return nextUri.PathAndQuery;
    }

    private sealed record MeshInboxResponse(string[]? Messages, MeshInboxLinks? Links);

    private sealed record MeshInboxLinks(string? Next);
}
