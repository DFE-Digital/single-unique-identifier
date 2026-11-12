using System.Security.Cryptography;
using SUI.NotificationService.Mesh.Configuration;

namespace SUI.NotificationService.Mesh.UnitTests.Configuration;

public sealed class MeshCertificateLoaderTests : IDisposable
{
    private readonly TestCertificates _certificates = new();

    public void Dispose() => _certificates.Dispose();

    [Fact]
    public void ShouldLoadRoot_WhenPem()
    {
        using var root = MeshCertificateLoader.LoadRootCertificate(_certificates.RootPemBase64());

        Assert.Equal(_certificates.Root.Thumbprint, root.Thumbprint);
    }

    [Fact]
    public void ShouldLoadRoot_WhenDer()
    {
        using var root = MeshCertificateLoader.LoadRootCertificate(
            TestCertificates.DerBase64(_certificates.Root)
        );

        Assert.Equal(_certificates.Root.Thumbprint, root.Thumbprint);
    }

    [Fact]
    public void ShouldThrow_WhenRootIsNotSelfSigned()
    {
        Assert.Throws<CryptographicException>(() =>
            MeshCertificateLoader.LoadRootCertificate(_certificates.IntermediatePemBase64())
        );
    }

    [Fact]
    public void ShouldReturnTrue_WhenIntermediateIsIssuedByRoot()
    {
        Assert.True(
            MeshCertificateLoader.IsIssuedBy(_certificates.Intermediate, _certificates.Root)
        );
    }

    [Fact]
    public void ShouldReturnFalse_WhenIntermediateIsIssuedByAnotherRoot()
    {
        using var other = new TestCertificates();

        Assert.False(MeshCertificateLoader.IsIssuedBy(_certificates.Intermediate, other.Root));
    }

    [Fact]
    public void ShouldLoadPfxWithPrivateKey_WhenPasswordCorrect()
    {
        using var certificate = MeshCertificateLoader.LoadClientCertificate(
            _certificates.PfxBase64(),
            TestCertificates.PfxPassword
        );

        Assert.True(certificate.HasPrivateKey);
        Assert.Equal(_certificates.Leaf.Thumbprint, certificate.Thumbprint);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void ShouldLoadPfx_WhenPasswordEmpty(string? password)
    {
        // Key Vault certificate shape: the secret holds a PFX with no password.
        using var certificate = MeshCertificateLoader.LoadClientCertificate(
            _certificates.PfxBase64(password: null),
            password
        );

        Assert.True(certificate.HasPrivateKey);
    }
}
