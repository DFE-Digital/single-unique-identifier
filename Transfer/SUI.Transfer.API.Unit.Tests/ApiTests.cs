using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using SUI.Transfer.Application.Models;
using SUI.Transfer.Application.Services;

namespace SUI.Transfer.API.Unit.Tests;

public class ApiTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly IFetchingService _mockFetchingService;
    private readonly HttpClient _client;

    private readonly JsonSerializerOptions _jsonSerializerOptions =
        new(JsonSerializerDefaults.Web)
        {
            Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
        };

    public ApiTests(WebApplicationFactory<Program> factory)
    {
        _mockFetchingService = Substitute.For<IFetchingService>();

        var appFactory = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                var descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(IFetchingService));
                if (descriptor != null) services.Remove(descriptor);

                services.AddSingleton<IFetchingService>(_ => _mockFetchingService);
            });
        });

        _client = appFactory.CreateClient();
    }

    [Fact]
    public async Task GetFetch_WhenFound_ReturnsOkResult()
    {
        // Arrange
        var testId = "999-000-1234";
        var mockResponse = new FetchResponse
        {
            Result = new FetchResult { Id = testId },
            Success = true,
        };
        
        _mockFetchingService.FetchAsync(Arg.Any<string>())
            .Returns(mockResponse);

        // Act
        var httpResponse = await _client.GetAsync("/api/v1/fetch/" + testId);

        // Assert
        Assert.Equal(HttpStatusCode.OK, httpResponse.StatusCode);
        var content = await httpResponse.Content.ReadFromJsonAsync<FetchResult>(_jsonSerializerOptions);
        Assert.NotNull(content);
        Assert.Equal(testId, content.Id);

    } 
    
    [Fact]
    public async Task GetFetch_WhenNotFound_ReturnsNotFoundResult()
    {
        // Arrange
        var testId = "999-000-1234";
        var mockResponse = new FetchResponse
        {
            Result = new FetchResult { Id = "000-000-0000" },
            Success = false,
        };
        
        _mockFetchingService.FetchAsync(Arg.Any<string>())
            .Returns(mockResponse);

        // Act
        var httpResponse = await _client.GetAsync("/api/v1/fetch/" + testId);

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, httpResponse.StatusCode);
    }
}