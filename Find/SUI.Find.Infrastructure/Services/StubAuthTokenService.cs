using System.Security.Cryptography;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SUi.Find.Application.Interfaces;
using SUI.Find.Infrastructure.Constants;
using SUI.Find.Infrastructure.Models;

namespace SUI.Find.Infrastructure.Services;

public class StubAuthTokenService(
    IOptions<AuthTokenServiceConfig> options,
    ILogger<StubAuthTokenService> logger,
    IHttpClientFactory httpClientFactory,
    ISecretService secretService) :
    AuthTokenService(options, logger, httpClientFactory, secretService)
{
    private readonly IOptions<AuthTokenServiceConfig> _options = options;

    private string? _privateKey;
    private string? _clientId;
    private string? _kid;

    protected override async Task EnsureInitializedAsync(CancellationToken cancellationToken)
    {
        if (_privateKey is not null) return;

        logger.LogInformation("First-time initialization: loading secrets from Azure Key Vault.");

        var privateKeyTask = await Task.FromResult(_options.Value.NHS_DIGITAL_PRIVATE_KEY);

        if (string.IsNullOrEmpty(privateKeyTask))
        {
            using var rsa = RSA.Create(2048);
            privateKeyTask = Convert.ToBase64String(rsa.ExportRSAPrivateKey());
        }
        else if (privateKeyTask.StartsWith("file:"))
        {
            privateKeyTask = privateKeyTask.Replace("file:", "");
            privateKeyTask = await File.ReadAllTextAsync(privateKeyTask);
        }

        var clientIdTask = await Task.FromResult(_options.Value.NHS_DIGITAL_CLIENT_ID);
        var kidTask = await Task.FromResult(_options.Value.NHS_DIGITAL_KID);


        _privateKey = privateKeyTask;
        _clientId = clientIdTask;
        _kid = kidTask;
    }
}