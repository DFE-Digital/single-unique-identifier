using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using SUI.Custodians.API.Client;
using SUI.Transfer.Application.Services;
using SUI.Transfer.Domain;
using SUI.Transfer.Domain.Consolidation;

namespace SUI.Transfer.IntegrationTests;

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
    public async Task PostTransfer_WithoutApiKey_ReturnsUnauthorized()
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
    public async Task PostTransfer_WithIncorrectApiKey_ReturnsUnauthorized()
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
    public async Task PostTransfer_WithCorrectApiKey_ReturnsOkResult()
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

    [Fact]
    public async Task GetTransferStatus_WithoutApiKey_ReturnsUnauthorized()
    {
        //Arrange
        var testJobId = Guid.Parse("7527627D-17AF-451B-9AF2-87E17E577F63");

        // Act
        var httpResponse = await _client.GetAsync($"/api/v1/transfer/{testJobId}");

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, httpResponse.StatusCode);
    }

    [Fact]
    public async Task GetTransferStatus_WithIncorrectApiKey_ReturnsUnauthorized()
    {
        //Arrange
        var testJobId = Guid.Parse("7527627D-17AF-451B-9AF2-87E17E577F63");

        _client.DefaultRequestHeaders.Add("X-Api-Key", "INCORRECT_API_KEY");

        // Act
        var httpResponse = await _client.GetAsync($"/api/v1/transfer/{testJobId}");

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, httpResponse.StatusCode);
    }

    [Fact]
    public async Task GetTransferStatus_WithUnusedJobId_ReturnsNotFoundResult()
    {
        //Arrange
        var testJobId = Guid.Parse("7527627D-17AF-451B-9AF2-87E17E577F63");

        _client.DefaultRequestHeaders.Add("X-Api-Key", _apiKey);

        // Act
        var httpResponse = await _client.GetAsync($"/api/v1/transfer/{testJobId}");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, httpResponse.StatusCode);
    }

    [Fact]
    public async Task GetTransferStatus_WithCompletedJobId_ReturnsOk_WithData()
    {
        //Arrange
        var testJobId = Guid.Parse("7527627D-17AF-451B-9AF2-87E17E577F63");
        var testSui = "999-000-1234";
        var createdAt = TimeProvider.System.GetUtcNow();
        var mockResponse = new CompletedTransferJobState(
            testJobId,
            testSui,
            CreateEmptyConformedConsolidatedData(testJobId, testSui, createdAt),
            createdAt,
            createdAt
        );

        _mockTransferService.GetTransferJobStateAsync(Arg.Any<Guid>()).Returns(mockResponse);

        _client.DefaultRequestHeaders.Add("X-Api-Key", _apiKey);

        // Act
        var httpResponse = await _client.GetAsync($"/api/v1/transfer/{testJobId}/");

        // Assert
        Assert.Equal(HttpStatusCode.OK, httpResponse.StatusCode);
        var content = await httpResponse.Content.ReadFromJsonAsync<CompletedTransferJobState>(
            _jsonSerializerOptions
        );
        Assert.NotNull(content);
        Assert.Equivalent(mockResponse, content);
    }

    [Fact]
    public async Task GetTransferStatus_WithUncompletedJobId_ReturnsOk()
    {
        //Arrange
        var testJobId = Guid.Parse("7527627D-17AF-451B-9AF2-87E17E577F63");
        var testSui = "999-000-1234";
        var createdAt = TimeProvider.System.GetUtcNow();
        var mockResponse = new RunningTransferJobState(testJobId, testSui, createdAt, createdAt);

        _mockTransferService.GetTransferJobStateAsync(Arg.Any<Guid>()).Returns(mockResponse);

        _client.DefaultRequestHeaders.Add("X-Api-Key", _apiKey);

        // Act
        var httpResponse = await _client.GetAsync($"/api/v1/transfer/{testJobId}/");

        // Assert
        Assert.Equal(HttpStatusCode.OK, httpResponse.StatusCode);
        var content = await httpResponse.Content.ReadFromJsonAsync<RunningTransferJobState>(
            _jsonSerializerOptions
        );
        Assert.NotNull(content);
        Assert.Equivalent(mockResponse, content);
    }

    [Fact]
    public async Task GetTransferResults_WithoutApiKey_ReturnsUnauthorized()
    {
        //Arrange
        var testJobId = Guid.Parse("7527627D-17AF-451B-9AF2-87E17E577F63");

        // Act
        var httpResponse = await _client.GetAsync($"/api/v1/transfer/{testJobId}/results");

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, httpResponse.StatusCode);
    }

    [Fact]
    public async Task GetTransferResults_WithIncorrectApiKey_ReturnsUnauthorized()
    {
        //Arrange
        var testJobId = Guid.Parse("7527627D-17AF-451B-9AF2-87E17E577F63");

        _client.DefaultRequestHeaders.Add("X-Api-Key", "INCORRECT_API_KEY");

        // Act
        var httpResponse = await _client.GetAsync($"/api/v1/transfer/{testJobId}/results");

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, httpResponse.StatusCode);
    }

    [Fact]
    public async Task GetTransferResults_WithUnusedJobId_ReturnsNotFound()
    {
        //Arrange
        var testJobId = Guid.Parse("7527627D-17AF-451B-9AF2-87E17E577F63");

        _client.DefaultRequestHeaders.Add("X-Api-Key", _apiKey);

        // Act
        var httpResponse = await _client.GetAsync($"/api/v1/transfer/{testJobId}/results");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, httpResponse.StatusCode);
    }

    [Fact]
    public async Task GetTransferResults_WithNotCompletedJobId_ReturnsBadRequest()
    {
        //Arrange
        var testJobId = Guid.Parse("7527627D-17AF-451B-9AF2-87E17E577F63");
        var testId = "999-000-1234";
        var createdAt = TimeProvider.System.GetUtcNow();
        var mockResponse = new QueuedTransferJobState(testJobId, testId, createdAt);

        _mockTransferService.GetTransferJobStateAsync(Arg.Any<Guid>()).Returns(mockResponse);

        _client.DefaultRequestHeaders.Add("X-Api-Key", _apiKey);

        // Act
        var httpResponse = await _client.GetAsync($"/api/v1/transfer/{testJobId}/results");

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, httpResponse.StatusCode);
    }

    [Fact]
    public async Task GetTransferResults_WithCompletedJobId_ReturnsOk()
    {
        //Arrange
        var testJobId = Guid.Parse("7527627D-17AF-451B-9AF2-87E17E577F63");
        var testSui = "999-000-1234";
        var createdAt = TimeProvider.System.GetUtcNow();
        var mockResponse = new CompletedTransferJobState(
            testJobId,
            testSui,
            CreateEmptyConformedConsolidatedData(testJobId, testSui, createdAt),
            createdAt,
            createdAt
        );

        _mockTransferService.GetTransferJobStateAsync(Arg.Any<Guid>()).Returns(mockResponse);

        _client.DefaultRequestHeaders.Add("X-Api-Key", _apiKey);

        // Act
        var httpResponse = await _client.GetAsync($"/api/v1/transfer/{testJobId}/results");

        // Assert
        Assert.Equal(HttpStatusCode.OK, httpResponse.StatusCode);
        var content = await httpResponse.Content.ReadFromJsonAsync<CompletedTransferJobState>(
            _jsonSerializerOptions
        );
        Assert.NotNull(content);
        Assert.Equivalent(mockResponse, content, true);
    }

    private static ConformedData CreateEmptyConformedConsolidatedData(
        Guid jobId,
        string sui,
        DateTimeOffset createdAt
    ) =>
        new(
            jobId,
            new ConsolidatedData(sui)
            {
                PersonalDetailsRecord = new PersonalDetailsRecordV1Consolidated(),
                ChildrensServicesDetailsRecord = new ChildSocialCareDetailsRecordV1Consolidated(),
                EducationDetailsRecord = new EducationDetailsRecordV1Consolidated(),
                HealthDataRecord = null,
                CrimeDataRecord = null,
                CountOfRecordsSuccessfullyFetched = 0,
                FailedFetches = [],
            },
            createdAt
        )
        {
            EducationAttendanceSummaries = new EducationAttendanceSummaries
            {
                CurrentAcademicYear = null,
                LastAcademicYear = new EducationAttendanceV1(),
            },
            HealthAttendanceSummaries = new HealthAttendanceSummaries
            {
                Last12Months = new HealthAttendanceSummary(1, 1, 1, 1),
                Last5Years = new HealthAttendanceSummary(5, 5, 5, 5),
            },
            ChildServicesReferralSummaries = null,
            CrimeMissingEpisodesSummaries = null,
            StatusFlags = null,
        };
}
