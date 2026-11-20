using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;

namespace SUI.NotificationService.Mesh.UnitTests;

/// <summary>
/// Generates a throwaway root → intermediate → leaf chain in memory. No real certificates.
/// </summary>
internal sealed class TestCertificates : IDisposable
{
    public const string PfxPassword = "test-pfx-password";

    public X509Certificate2 Root { get; }
    public X509Certificate2 Intermediate { get; }
    public X509Certificate2 Leaf { get; }

    public TestCertificates()
    {
        var notBefore = DateTimeOffset.UtcNow.AddMinutes(-5);
        var notAfter = DateTimeOffset.UtcNow.AddDays(1);

        using var rootKey = RSA.Create(2048);
        var rootRequest = CreateCaRequest("CN=Test MESH Root", rootKey);
        Root = rootRequest.CreateSelfSigned(notBefore, notAfter);

        using var intermediateKey = RSA.Create(2048);
        var intermediateRequest = CreateCaRequest("CN=Test MESH Intermediate", intermediateKey);
        using (var intermediatePublic = intermediateRequest.Create(Root, notBefore, notAfter, [1]))
        {
            Intermediate = intermediatePublic.CopyWithPrivateKey(intermediateKey);
        }

        using var leafKey = RSA.Create(2048);
        var leafRequest = new CertificateRequest(
            "CN=Test MESH Client",
            leafKey,
            HashAlgorithmName.SHA256,
            RSASignaturePadding.Pkcs1
        );
        using var leafPublic = leafRequest.Create(Intermediate, notBefore, notAfter, [2]);
        Leaf = leafPublic.CopyWithPrivateKey(leafKey);
    }

    public string PfxBase64(string? password = PfxPassword) =>
        Convert.ToBase64String(Leaf.Export(X509ContentType.Pkcs12, password));

    public string RootPemBase64() => PemBase64(Root);

    public string IntermediatePemBase64() => PemBase64(Intermediate);

    public static string PemBase64(X509Certificate2 certificate) =>
        Convert.ToBase64String(Encoding.UTF8.GetBytes(certificate.ExportCertificatePem()));

    public static string DerBase64(X509Certificate2 certificate) =>
        Convert.ToBase64String(certificate.RawData);

    public void Dispose()
    {
        Leaf.Dispose();
        Intermediate.Dispose();
        Root.Dispose();
    }

    private static CertificateRequest CreateCaRequest(string subject, RSA key)
    {
        var request = new CertificateRequest(
            subject,
            key,
            HashAlgorithmName.SHA256,
            RSASignaturePadding.Pkcs1
        );
        request.CertificateExtensions.Add(new X509BasicConstraintsExtension(true, false, 0, true));
        request.CertificateExtensions.Add(
            new X509KeyUsageExtension(X509KeyUsageFlags.KeyCertSign, true)
        );
        return request;
    }
}
