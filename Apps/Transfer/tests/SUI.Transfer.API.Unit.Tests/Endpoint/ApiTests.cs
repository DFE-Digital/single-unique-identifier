using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using SUI.Transfer.Application.Services;
using SUI.Transfer.Domain;

namespace SUI.Transfer.API.Unit.Tests.Endpoint;

public class ApiTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly ITransferService _mockTransferService;
    private readonly HttpClient _client;

    private readonly JsonSerializerOptions _jsonSerializerOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) },
    };

    private readonly string _apiKey;

    public ApiTests(WebApplicationFactory<Program> factory)
    {
        _mockTransferService = Substitute.For<ITransferService>();

        var appFactory = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                var descriptor = services.SingleOrDefault(d =>
                    d.ServiceType == typeof(ITransferService)
                );
                if (descriptor != null)
                    services.Remove(descriptor);

                services.AddSingleton<ITransferService>(_ => _mockTransferService);
            });
        });

        _client = appFactory.CreateClient(new WebApplicationFactoryClientOptions());

        var config = InitConfiguration();
        _apiKey = config["Authentication:ApiKey"] ?? string.Empty;
    }

    private static IConfiguration InitConfiguration()
    {
        var config = new ConfigurationBuilder().AddJsonFile("appsettings.Test.json").Build();
        return config;
    }

    [Fact]
    public async Task GetTransfer_WithoutApiKey_ReturnsUnauthorized()
    {
        //Arrange
        var testId = "999-000-1234";

        // Act
        var httpResponse = await _client.PostAsJsonAsync(
            "/api/v1/transfer/",
            testId,
            _jsonSerializerOptions
        );

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, httpResponse.StatusCode);
    }

    [Fact]
    public async Task GetTransfer_WithIncorrectApiKey_ReturnsUnauthorized()
    {
        //Arrange
        var testId = "999-000-1234";

        _client.DefaultRequestHeaders.Add("X-Api-Key", "INCORRECT_API_KEY");

        // Act
        var httpResponse = await _client.PostAsJsonAsync(
            "/api/v1/transfer/",
            testId,
            _jsonSerializerOptions
        );

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, httpResponse.StatusCode);
    }

    [Fact]
    public async Task GetTransfer_WithCorrectApiKey_ReturnsOkResult()
    {
        // Arrange
        var testId = "999-000-1234";
        var createdAt = TimeProvider.System.GetUtcNow();
        var mockResponse = new QueuedTransferJobState(Guid.NewGuid(), testId, createdAt);

        _mockTransferService.BeginTransferJob(Arg.Any<string>()).Returns(mockResponse);

        _client.DefaultRequestHeaders.Add("X-Api-Key", _apiKey);

        // Act
        var httpResponse = await _client.PostAsJsonAsync(
            "/api/v1/transfer/",
            testId,
            _jsonSerializerOptions
        );

        // Assert
        Assert.Equal(HttpStatusCode.OK, httpResponse.StatusCode);
        var content = await httpResponse.Content.ReadFromJsonAsync<QueuedTransferJobState>(
            _jsonSerializerOptions
        );
        Assert.NotNull(content);
        Assert.Equal(
            new QueuedTransferJobState(mockResponse.JobId, testId, createdAt)
            {
                LastUpdatedAt = content.LastUpdatedAt,
            },
            content
        );
    }
}
