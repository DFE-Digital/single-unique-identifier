using System.Net;
using System.Text;
using System.Text.Json;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using NSubstitute;
using SUI.Find.Application.Dtos;
using SUI.Find.Application.Interfaces;
using SUI.Find.Application.Models;
using SUI.Find.Domain.Models;
using SUI.Find.FindApi.Functions.ActivityFunctions;

namespace SUI.Find.FindApi.UnitTests.FunctionTests;

public class QueryProvidersFunctionTests
{
    private readonly ILogger<QueryProvidersFunction> _mockLogger = Substitute.For<
        ILogger<QueryProvidersFunction>
    >();
    private readonly IHttpClientFactory _httpClientFactory = Substitute.For<IHttpClientFactory>();
    private readonly IPersonIdEncryptionService _encryptionService =
        Substitute.For<IPersonIdEncryptionService>();
    private readonly IMaskUrlService _maskUrlService = Substitute.For<IMaskUrlService>();
    private readonly FunctionContext _mockContext;
    private readonly QueryProvidersFunction _function;
    private readonly IOutboundAuthService _outboundAuthService;

    private class MockHttpMessageHandler : HttpMessageHandler
    {
        private readonly Func<
            HttpRequestMessage,
            CancellationToken,
            Task<HttpResponseMessage>
        > _handlerFunc;

        public MockHttpMessageHandler(
            Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> handlerFunc
        )
        {
            _handlerFunc = handlerFunc;
        }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken
        )
        {
            return _handlerFunc(request, cancellationToken);
        }
    }

    private static ProviderDefinition MockProvider(
        string orgId = "test-org-1",
        string providerName = "Test Provider",
        EncryptionDefinition? encryption = null,
        string url = "https://test-provider.com/api/records"
    )
    {
        return new ProviderDefinition
        {
            OrgId = orgId,
            ProviderName = providerName,
            Encryption = encryption,
            Connection = new ConnectionDefinition { Url = url },
        };
    }

    public QueryProvidersFunctionTests()
    {
        var httpClient = new HttpClient(
            new MockHttpMessageHandler(
                (_, _) =>
                {
                    var response = new HttpResponseMessage(HttpStatusCode.OK)
                    {
                        Content = new StringContent("[]", Encoding.UTF8, "application/json"),
                    };
                    return Task.FromResult(response);
                }
            )
        );

        _httpClientFactory.CreateClient(Arg.Any<string>()).Returns(httpClient);
        _mockContext = Substitute.For<FunctionContext>();
        _outboundAuthService = Substitute.For<IOutboundAuthService>();
        _function = new QueryProvidersFunction(
            _mockLogger,
            _httpClientFactory,
            _encryptionService,
            _maskUrlService,
            _outboundAuthService
        );
    }

    [Fact]
    public async Task QueryProvider_ReturnsEmptyList_WhenEncryptionIsNull()
    {
        // Arrange
        var provider = MockProvider();

        var input = new QueryProviderInput(
            "client-id-1",
            "job-id-1",
            "invocation-id",
            "1234567890123456",
            provider
        );

        // Act
        // Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _function.QueryProvider(_mockContext, input)
        );
    }

    [Fact]
    public async Task QueryProvider_ReturnsEmptyList_WhenEncryptionFails()
    {
        // Arrange
        var provider = MockProvider(
            encryption: new EncryptionDefinition
            {
                Key = "test-key-1",
                KeyId = "key-1",
                Algorithm = "AES",
            }
        );

        var input = new QueryProviderInput(
            "client-id-1",
            "job-id-1",
            "invocation-id",
            "1234567890123456",
            provider
        );

        _encryptionService
            .EncryptNhsToPersonId(Arg.Any<string>(), Arg.Any<EncryptionDefinition>())
            .Returns(Result<string>.Fail("Encryption failed"));

        // Act
        var result = await _function.QueryProvider(_mockContext, input);

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public async Task QueryProvider_ReturnsMaskedResults_WhenSuccessFull()
    {
        // arrange
        var provider = MockProvider(
            encryption: new EncryptionDefinition
            {
                Key = "test-key",
                KeyId = "key-1",
                Algorithm = "AES",
            }
        );
        var input = new QueryProviderInput(
            "client-id-1",
            "job-id-1",
            "invocation-id",
            "1234567890123456",
            provider
        );
        var providerResponse = new List<SearchResultItem>
        {
            new("SystemA", "Provider A", "Type1", "/v1/records/original-id"),
        };
        var jsonResponse = JsonSerializer.Serialize(providerResponse);
        var maskedItems = new List<SearchResultItem>
        {
            new("SystemA", "Provider A", "Type1", "/v1/records/masked-id"),
        };

        _encryptionService
            .EncryptNhsToPersonId(Arg.Any<string>(), Arg.Any<EncryptionDefinition>())
            .Returns(Result<string>.Ok("encrypted-person-id"));

        var successClient = new HttpClient(
            new MockHttpMessageHandler(
                (_, _) =>
                    Task.FromResult(
                        new HttpResponseMessage(HttpStatusCode.OK)
                        {
                            Content = new StringContent(
                                jsonResponse,
                                Encoding.UTF8,
                                "application/json"
                            ),
                        }
                    )
            )
        );

        _httpClientFactory.CreateClient(Arg.Any<string>()).Returns(successClient);
        _outboundAuthService
            .GetAccessTokenAsync(provider, Arg.Any<CancellationToken>())
            .Returns(Result<string>.Ok("access-token"));
        _maskUrlService
            .CreateAsync(Arg.Any<List<SearchResultItem>>(), input, Arg.Any<CancellationToken>())
            .Returns(maskedItems);

        // Act
        var result = await _function.QueryProvider(_mockContext, input);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("/v1/records/masked-id", result.First().RecordUrl);
    }

    [Fact]
    public async Task QueryProvider_ReturnsEmptyList_WhenUnsuccessful()
    {
        // Arrange
        var provider = MockProvider(
            encryption: new EncryptionDefinition
            {
                Key = "test-key",
                KeyId = "key-1",
                Algorithm = "AES",
            }
        );
        var input = new QueryProviderInput(
            "client-id",
            "job-id",
            "invocation-id",
            "12345",
            provider
        );

        _encryptionService
            .EncryptNhsToPersonId(Arg.Any<string>(), Arg.Any<EncryptionDefinition>())
            .Returns(Result<string>.Ok("encrypted-person-id"));

        _outboundAuthService
            .GetAccessTokenAsync(provider, Arg.Any<CancellationToken>())
            .Returns(Result<string>.Ok("access-token"));

        var failedClient = new HttpClient(
            new MockHttpMessageHandler(
                (_, _) =>
                    Task.FromResult(new HttpResponseMessage(HttpStatusCode.InternalServerError))
            )
        );

        _httpClientFactory.CreateClient(Arg.Any<string>()).Returns(failedClient);

        // Act
        var result = await _function.QueryProvider(_mockContext, input);

        // Assert
        Assert.Empty(result);
    }
}
