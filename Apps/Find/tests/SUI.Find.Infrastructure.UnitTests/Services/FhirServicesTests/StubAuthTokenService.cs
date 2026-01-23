using System.Diagnostics.CodeAnalysis;
using System.Security.Cryptography;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SUI.Find.Infrastructure.Interfaces;
using SUI.Find.Infrastructure.Models.Fhir;
using SUI.Find.Infrastructure.Services.Fhir;

namespace SUI.Find.Infrastructure.UnitTests.Services.FhirServicesTests;

/// <summary>
/// Override AuthTokenService to use local keys instead of Key Vault.
/// Creates a random key if none specified (for testing purposes).
/// </summary>
/// <param name="options"></param>
/// <param name="logger"></param>
/// <param name="httpClientFactory"></param>
/// <param name="secretService"></param>
[ExcludeFromCodeCoverage(Justification = "Stub for local development & testing purposes")]
public class StubAuthTokenService(
    IOptions<AuthTokenServiceConfig> options,
    ILogger<StubAuthTokenService> logger,
    IHttpClientFactory httpClientFactory,
    ISecretService secretService
) : AuthTokenService(options, logger, httpClientFactory, secretService)
{
    private readonly IOptions<AuthTokenServiceConfig> _options = options;

    protected override async Task EnsureInitializedAsync(CancellationToken cancellationToken)
    {
        if (PrivateKey is not null)
            return;

        var privateKeyTask = _options.Value.NHS_DIGITAL_PRIVATE_KEY;

        if (string.IsNullOrEmpty(privateKeyTask))
        {
            using var rsa = RSA.Create(2048);
            privateKeyTask = Convert.ToBase64String(rsa.ExportRSAPrivateKey());
        }
        else if (privateKeyTask.StartsWith("file:"))
        {
            privateKeyTask = privateKeyTask.Replace("file:", "");
            privateKeyTask = await File.ReadAllTextAsync(privateKeyTask, cancellationToken);
        }

        var clientIdTask = _options.Value.NHS_DIGITAL_CLIENT_ID;
        var kidTask = _options.Value.NHS_DIGITAL_KID;

        PrivateKey = privateKeyTask;
        ClientId = clientIdTask;
        Kid = kidTask;
    }
}
