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

    /// <summary>
    /// Base64 of the mutual-TLS client PFX (certificate and private key). Required when
    /// <see cref="AcceptLocalDevCert"/> is false. A Key Vault certificate's same-named secret
    /// already has this shape.
    /// </summary>
    public string? MailboxMTLSPfx { get; init; }

    /// <summary>Optional password for <see cref="MailboxMTLSPfx"/>. Empty for a Key Vault certificate.
    /// Only needed for Localhost if testing online MESH mailbox
    /// </summary>
    public string? MailboxMTLSPfxPassword { get; init; }

    /// <summary>
    /// Base64 of the NHS root CA certificate (PEM or DER), e.g. NHS INT Root G2. The only trust
    /// anchor for MESH's server certificate. Required when <see cref="AcceptLocalDevCert"/> is false.
    /// </summary>
    public string? MailboxRootCA { get; init; }

    /// <summary>
    /// Base64 of the NHS intermediate CA certificate (PEM or DER) issued by
    /// <see cref="MailboxRootCA"/>, e.g. NHS INT Authentication G2. Used to build the server chain and
    /// sent with the client certificate. Required when <see cref="AcceptLocalDevCert"/> is false.
    /// </summary>
    public string? MailboxIntermediateCA { get; init; }
}
