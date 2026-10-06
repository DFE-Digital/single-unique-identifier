using Microsoft.Extensions.Options;
using SUI.NotificationService.Mesh.Configuration;

namespace SUI.NotificationService.Mesh.Handlers;

public class NhsMeshAuthHandler : DelegatingHandler
{
    private readonly NhsMeshConfig _config;

    public NhsMeshAuthHandler(IOptions<NhsMeshConfig> options) => _config = options.Value;

    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken
    )
    {
        request.Headers.Remove("Authorization");
        request.Headers.Add(
            "Authorization",
            MeshAuthHeaderBuilder.Build(
                _config.MailboxId,
                _config.MailboxPassword,
                _config.SharedKey
            )
        );
        return base.SendAsync(request, cancellationToken);
    }
}
