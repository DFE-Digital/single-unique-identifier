using System.Security.Cryptography;
using System.Text;
using SUI.NotificationService.Mesh.Handlers;

namespace SUI.NotificationService.Mesh.UnitTests.Handlers;

public sealed class MeshAuthHeaderBuilderTests
{
    private const string MailboxId = "X26ABC1";
    private const string MailboxPassword = "password";
    private const string SharedKey = "shared-key";

    [Fact]
    public void Build_ReturnsNhsMeshHeader_WithHashOfTheMailboxCredentials()
    {
        var header = MeshAuthHeaderBuilder.Build(MailboxId, MailboxPassword, SharedKey);

        // NHSMESH {mailbox_id}:{nonce}:{nonce_count}:{timestamp}:{hash}
        Assert.StartsWith("NHSMESH ", header);
        var parts = header["NHSMESH ".Length..].Split(':');
        Assert.Equal(5, parts.Length);

        var nonce = parts[1];
        var timestamp = parts[3];
        Assert.Equal(MailboxId, parts[0]);
        Assert.True(Guid.TryParse(nonce, out _));
        Assert.Equal("1", parts[2]);
        Assert.Matches("^[0-9]{12}$", timestamp);

        // The hash covers the password, which is why it never appears in the header itself.
        var expectedHash = ExpectedHash($"{MailboxId}:{nonce}:1:{MailboxPassword}:{timestamp}");
        Assert.Equal(expectedHash, parts[4]);
    }

    [Fact]
    public void Build_UsesAFreshNonce_OnEveryCall()
    {
        var first = MeshAuthHeaderBuilder.Build(MailboxId, MailboxPassword, SharedKey);
        var second = MeshAuthHeaderBuilder.Build(MailboxId, MailboxPassword, SharedKey);

        Assert.NotEqual(first.Split(':')[1], second.Split(':')[1]);
    }

    private static string ExpectedHash(string message)
    {
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(SharedKey));
        return Convert
            .ToHexString(hmac.ComputeHash(Encoding.UTF8.GetBytes(message)))
            .ToLowerInvariant();
    }
}
