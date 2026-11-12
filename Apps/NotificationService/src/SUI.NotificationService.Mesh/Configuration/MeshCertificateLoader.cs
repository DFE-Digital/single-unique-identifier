using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace SUI.NotificationService.Mesh.Configuration;

/// <summary>
/// Loads the MESH mutual-TLS material from base64 config values. Shared by startup validation and
/// the primary handler so both read the certificates the same way. Exception messages from here
/// must never be logged: callers report only which key failed.
/// </summary>
internal static class MeshCertificateLoader
{
    /// <summary>Loads the client certificate and its private key from a base64 PFX.</summary>
    /// <exception cref="FormatException">The value is not valid base64.</exception>
    /// <exception cref="CryptographicException">The PFX is invalid, the password is wrong, or it has no private key.</exception>
    public static X509Certificate2 LoadClientCertificate(string pfxBase64, string? password)
    {
        var certificate = X509CertificateLoader.LoadPkcs12(
            Convert.FromBase64String(pfxBase64),
            string.IsNullOrEmpty(password) ? null : password
        );

        if (!certificate.HasPrivateKey)
        {
            certificate.Dispose();
            throw new CryptographicException("The client certificate has no private key.");
        }

        return certificate;
    }

    /// <summary>Loads a base64 PEM or DER certificate that must be a self-signed root.</summary>
    /// <exception cref="FormatException">The value is not valid base64.</exception>
    /// <exception cref="CryptographicException">The certificate is invalid or not self-signed.</exception>
    public static X509Certificate2 LoadRootCertificate(string certificateBase64)
    {
        var certificate = LoadCertificate(certificateBase64);

        if (!certificate.SubjectName.RawData.AsSpan().SequenceEqual(certificate.IssuerName.RawData))
        {
            certificate.Dispose();
            throw new CryptographicException("The root CA certificate is not self-signed.");
        }

        return certificate;
    }

    /// <summary>Loads a single base64 PEM or DER certificate.</summary>
    /// <exception cref="FormatException">The value is not valid base64.</exception>
    /// <exception cref="CryptographicException">The certificate is invalid.</exception>
    public static X509Certificate2 LoadCertificate(string certificateBase64) =>
        X509CertificateLoader.LoadCertificate(Convert.FromBase64String(certificateBase64));

    /// <summary>
    /// True when <paramref name="intermediate"/> chains directly to <paramref name="root"/>.
    /// Revocation is not checked here: this only proves the two config values belong together.
    /// </summary>
    public static bool IsIssuedBy(X509Certificate2 intermediate, X509Certificate2 root)
    {
        using var chain = new X509Chain();
        chain.ChainPolicy.TrustMode = X509ChainTrustMode.CustomRootTrust;
        chain.ChainPolicy.CustomTrustStore.Add(root);
        chain.ChainPolicy.RevocationMode = X509RevocationMode.NoCheck;

        return chain.Build(intermediate)
            && chain.ChainElements.Count == 2
            && chain.ChainElements[1].Certificate.RawData.AsSpan().SequenceEqual(root.RawData);
    }
}
