using System.ComponentModel.DataAnnotations;

namespace SUI.NotificationService.Mesh.Configuration;

public class NhsMeshConfig
{
    public const string SectionName = "NhsMeshConfig";

    [Required]
    [Url]
    public required string MailboxBaseUrl { get; set; }

    [Required]
    public required string SharedKey { get; set; }

    [Required]
    public required string MailboxId { get; set; }

    [Required]
    public required string MailboxPassword { get; set; }

    /// <summary>
    /// Accepts the self-signed certificate presented by the local MESH sandbox.
    /// This MUST be false when not in CI or local development; validation rejects it unless
    /// <see cref="MailboxBaseUrl"/> is a loopback address.
    /// </summary>
    public bool AcceptLocalDevCert { get; init; } = false;
}
