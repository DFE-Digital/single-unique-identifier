using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using Microsoft.Extensions.Options;

namespace SUI.NotificationService.Mesh.Configuration;

/// <summary>
/// Requires the mutual-TLS client certificate and the NHS CA certificates whenever the sandbox
/// shortcut is off. Failure messages name the key only: <c>NotificationServiceRunner</c> logs them,
/// so they must never contain a config value or a parser exception message.
/// </summary>
internal sealed class NhsMeshCertificateValidator : IValidateOptions<NhsMeshConfig>
{
    public ValidateOptionsResult Validate(string? name, NhsMeshConfig options)
    {
        if (options.AcceptLocalDevCert)
        {
            return ValidateOptionsResult.Skip;
        }

        var failures = new List<string>();

        using var clientCertificate = LoadOrReport(
            options.MailboxMTLSPfx,
            nameof(NhsMeshConfig.MailboxMTLSPfx),
            pfx => MeshCertificateLoader.LoadClientCertificate(pfx, options.MailboxMTLSPfxPassword),
            $"it must be a base64 PFX with a private key, matching {nameof(NhsMeshConfig.MailboxMTLSPfxPassword)}.",
            failures
        );

        using var root = LoadOrReport(
            options.MailboxRootCA,
            nameof(NhsMeshConfig.MailboxRootCA),
            MeshCertificateLoader.LoadRootCertificate,
            "it must be a base64 PEM or DER self-signed root certificate.",
            failures
        );

        using var intermediate = LoadOrReport(
            options.MailboxIntermediateCA,
            nameof(NhsMeshConfig.MailboxIntermediateCA),
            MeshCertificateLoader.LoadCertificate,
            "it must be a base64 PEM or DER certificate.",
            failures
        );

        if (
            root is not null
            && intermediate is not null
            && !MeshCertificateLoader.IsIssuedBy(intermediate, root)
        )
        {
            failures.Add(
                $"{nameof(NhsMeshConfig.MailboxIntermediateCA)} is not issued by {nameof(NhsMeshConfig.MailboxRootCA)}."
            );
        }

        return failures.Count == 0
            ? ValidateOptionsResult.Success
            : ValidateOptionsResult.Fail(failures);
    }

    private static X509Certificate2? LoadOrReport(
        string? value,
        string key,
        Func<string, X509Certificate2> load,
        string expectation,
        List<string> failures
    )
    {
        if (IsMissing(value, key, failures))
        {
            return null;
        }

        try
        {
            return load(value!);
        }
        catch (Exception exception) when (exception is FormatException or CryptographicException)
        {
            failures.Add($"{key} could not be loaded: {expectation}");
            return null;
        }
    }

    private static bool IsMissing(string? value, string key, List<string> failures)
    {
        if (!string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        failures.Add(
            $"{key} is required when {nameof(NhsMeshConfig.AcceptLocalDevCert)} is false."
        );
        return true;
    }
}
