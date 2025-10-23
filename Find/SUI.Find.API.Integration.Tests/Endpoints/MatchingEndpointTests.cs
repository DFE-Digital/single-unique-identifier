using NSubstitute;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SUi.Find.Application.Common;
using SUi.Find.Application.Interfaces;
using SUi.Find.Application.Models;
using SUi.Find.Application.Services;
using SUI.Find.Domain.Enums;

namespace SUI.Find.API.Integration.Tests.Endpoints;

public class MatchEndpointIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;
    private readonly IFhirService _fhirService;

    private readonly JsonSerializerOptions _jsonSerializerOptions =
        new(JsonSerializerDefaults.Web)
        {
            Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
        };

    public MatchEndpointIntegrationTests(WebApplicationFactory<Program> factory)
    {
        var logger = Substitute.For<ILogger<MatchingService>>();
        var searchIdService = Substitute.For<ISearchIdService>();
        _fhirService = Substitute.For<IFhirService>();
        IMatchingService matchingService = new MatchingService(logger, _fhirService, searchIdService);
        var authTokenService = Substitute.For<IAuthTokenService>();


        var appFactory = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                services.AddSingleton<IMatchingService>(_ => matchingService);
                services.AddSingleton<IFhirService>(_ => _fhirService);
                services.AddSingleton<ISearchIdService>(_ => searchIdService);
                services.AddSingleton<IAuthTokenService>(_ => authTokenService);
            });
        });
        _client = appFactory.CreateClient();
    }

    /// <summary>
    /// Tests that a POST request to the /api/v1/matchperson endpoint returns an OK result when a match is found.
    ///  integrating with mocked FHIR and matching services.
    /// </summary>
    [Theory]
    [InlineData(0.95)]
    public async Task Post_MatchPerson_ReturnsOkResult(decimal score)
    {
        // Arr
        var fhirSearchResult = SearchResult.Match("1234567890", score);

        var requestModel = new PersonSpecification
        {
            Given = "Jon",
            Family = "Smith",
            BirthDate = new DateOnly(DateTime.Now.AddYears(-10).Year, 1, 1)
        };

        // mock response from fhir endpoint service
        var fhirResponse = Result<SearchResult>.Success(fhirSearchResult);

        _fhirService.PerformSearchAsync(Arg.Any<SearchQuery>())
            .Returns(fhirResponse);

        // Act
        var response = await _client.PostAsync("/api/v1/matchperson", JsonContent.Create(requestModel),
            TestContext.Current.CancellationToken);

        // Assert
        await _fhirService.Received(1).PerformSearchAsync(Arg.Any<SearchQuery>());
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var content =
            await response.Content.ReadFromJsonAsync<PersonMatchResponse>(_jsonSerializerOptions,
                CancellationToken.None);

        Assert.NotNull(content);
        Assert.NotNull(content.Result);
        Assert.Equal(MatchStatus.Match, content.Result.MatchStatus);
        Assert.Equal("1234567890", content.Result.NhsNumber);
        Assert.Equal(score, content.Result.Score);
    }
}