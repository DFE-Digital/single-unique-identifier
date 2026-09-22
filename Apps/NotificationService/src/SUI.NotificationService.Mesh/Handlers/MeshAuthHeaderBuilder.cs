using System.Security.Cryptography;
using System.Text;

namespace SUI.NotificationService.Mesh.Handlers;

public static class MeshAuthHeaderBuilder
{
    public static string Build(string mailboxId, string mailboxPassword, string sharedKey)
    {
        var nonce = Guid.NewGuid().ToString();
        const int nonceCount = 1;
        var timestamp = DateTime.UtcNow.ToString("yyyyMMddHHmm");

        var message = $"{mailboxId}:{nonce}:{nonceCount}:{mailboxPassword}:{timestamp}";

        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(sharedKey));
        var hashBytes = hmac.ComputeHash(Encoding.UTF8.GetBytes(message));
        var hash = Convert.ToHexString(hashBytes).ToLowerInvariant();

        return $"NHSMESH {mailboxId}:{nonce}:{nonceCount}:{timestamp}:{hash}";
    }
}
