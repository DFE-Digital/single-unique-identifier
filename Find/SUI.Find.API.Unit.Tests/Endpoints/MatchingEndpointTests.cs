using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using SUi.Find.Application.Interfaces;
using SUi.Find.Application.Models;
using SUi.Find.Application.Validation;
using SUI.Find.Domain.Enums;

namespace SUI.Find.API.Unit.Tests.Endpoints;

public class MatchingEndpointTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly IMatchingService _mockMatchingService;
    private readonly HttpClient _client;

    private readonly JsonSerializerOptions _jsonSerializerOptions =
        new(JsonSerializerDefaults.Web)
        {
            Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
        };

    public MatchingEndpointTests(WebApplicationFactory<Program> factory)
    {
        _mockMatchingService = Substitute.For<IMatchingService>();

        var appFactory = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                var descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(IMatchingService));
                if (descriptor != null) services.Remove(descriptor);

                services.AddSingleton<IMatchingService>(_ => _mockMatchingService);
            });
        });

        _client = appFactory.CreateClient();
    }
    
    [Fact]
    public async Task PostMatchPerson_WhenMatchFound_ReturnsOkResult()
    {
        // Arrange
        var testModel = new PersonSpecification { Given = "Test", Family = "User" };
        var matchResult = new MatchResult(MatchStatus.Match, 0.95m, "test", "1234567890");
        var mockResponse = new PersonMatchResponse
        {
            Result = matchResult, DataQuality = new DataQualityResult { Given = QualityType.Valid }
        };
        _mockMatchingService.SearchAsync(Arg.Any<PersonSpecification>())
            .Returns(mockResponse);

        // Act
        var httpResponse =
            await _client.PostAsJsonAsync("/api/v1/matchperson", testModel, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.OK, httpResponse.StatusCode);
        var content =
            await httpResponse.Content.ReadFromJsonAsync<PersonMatchResponse>(_jsonSerializerOptions,
                TestContext.Current.CancellationToken);
        Assert.NotNull(content?.Result);
        Assert.Equal(MatchStatus.Match, content.Result.MatchStatus);
        Assert.Equal("1234567890", content.Result.NhsNumber);
    }
    
    [Fact]
    public async Task PostMatchPerson_WhenServiceReturnsError_ReturnsBadRequestResult()
    {
        // Arrange
        var testModel = new PersonSpecification { Given = "Test", Family = "User" };
        var errorResult = new MatchResult(MatchStatus.Error, errorMessage: "Invalid input");
        var mockResponse = new PersonMatchResponse
            { Result = errorResult, DataQuality = new DataQualityResult { Given = QualityType.Valid } };

        _mockMatchingService.SearchAsync(Arg.Any<PersonSpecification>())
            .Returns(mockResponse);

        // Act
        var httpResponse =
            await _client.PostAsJsonAsync("/api/v1/matchperson", testModel, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, httpResponse.StatusCode);

        var content =
            await httpResponse.Content.ReadFromJsonAsync<PersonMatchResponse>(_jsonSerializerOptions,
                TestContext.Current.CancellationToken);
        Assert.NotNull(content?.Result);
        Assert.Equal(MatchStatus.Error, content.Result.MatchStatus);
        Assert.Equal("Invalid input", content.Result.ErrorMessage);
    }
}