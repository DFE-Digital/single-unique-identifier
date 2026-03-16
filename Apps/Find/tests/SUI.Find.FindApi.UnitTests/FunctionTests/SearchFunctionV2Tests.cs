using System.Net;
using System.Text.Json;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSubstitute;
using SUI.Find.Application.Configurations;
using SUI.Find.Application.Constants;
using SUI.Find.Application.Enums;
using SUI.Find.Application.Models;
using SUI.Find.FindApi.Functions.HttpFunctions;
using SUI.Find.FindApi.Models;
using SUI.Find.FindApi.UnitTests.Mocks;
using SUI.Find.Infrastructure.Interfaces;
using SUI.Find.Infrastructure.Models;

namespace SUI.Find.FindApi.UnitTests.FunctionTests;

public class SearchFunctionV2Tests
{
    private readonly ILogger<SearchFunctionV2> _logger = Substitute.For<
        ILogger<SearchFunctionV2>
    >();
    private readonly IJobQueueService _findQueueService = Substitute.For<IJobQueueService>();
    private readonly IOptions<EncryptionConfiguration> _encryptionConfig = Substitute.For<
        IOptions<EncryptionConfiguration>
    >();
    private readonly SearchFunctionV2 _function;

    public SearchFunctionV2Tests()
    {
        _encryptionConfig.Value.Returns(
            new EncryptionConfiguration { EnablePersonIdEncryption = false }
        );
        _function = new SearchFunctionV2(_logger, _findQueueService, _encryptionConfig);
    }

    [Fact]
    public async Task SearchesV2_ShouldReturnUnauthorized_WhenAuthContextIsMissing()
    {
        // Arrange
        var request = MockHttpRequestData.CreateJson(new StartSearchRequest("1234567890"));
        var context = Substitute.For<FunctionContext>();

        // Act
        var result = await _function.SearchesV2(request, context, CancellationToken.None);

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, result.StatusCode);
    }

    [Fact]
    public async Task SearchesV2_ShouldReturnBadRequest_WhenRequestIsInvalid()
    {
        // Arrange
        var requestData = new StartSearchRequest(""); // Invalid SUID
        var request = MockHttpRequestData.CreateJson(requestData);
        var context = CreateContextWithAuth("test-client");

        // Act
        var result = await _function.SearchesV2(request, context, CancellationToken.None);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, result.StatusCode);
    }

    [Fact]
    public async Task SearchesV2_ShouldReturnAccepted_WhenRequestIsValid()
    {
        // Arrange
        var requestData = new StartSearchRequest("9000000009"); // Assuming valid NHS number
        var request = MockHttpRequestData.CreateJson(requestData);
        var context = CreateContextWithAuth("test-client");

        var expectedJobId = Guid.NewGuid().ToString();
        var searchJobDto = new SearchJobDto
        {
            JobId = expectedJobId,
            PersonId = "9000000009",
            Status = SearchStatus.Queued,
            CreatedAt = DateTime.UtcNow,
            LastUpdatedAt = DateTime.UtcNow,
        };

        _findQueueService
            .PostSearchJobAsync(Arg.Any<SearchRequestMessage>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(searchJobDto));

        // Act
        var result = await _function.SearchesV2(request, context, CancellationToken.None);

        // Assert
        Assert.Equal(HttpStatusCode.Accepted, result.StatusCode);
        await _findQueueService
            .Received(1)
            .PostSearchJobAsync(
                Arg.Is<SearchRequestMessage>(m =>
                    m.PersonId == "9000000009" && m.RequestingCustodianId == "test-client"
                ),
                Arg.Any<CancellationToken>()
            );

        // Assert the response body
        result.Body.Position = 0;
        using var reader = new StreamReader(result.Body);
        var responseBody = await reader.ReadToEndAsync();
        var searchJob = JsonSerializer.Deserialize<SearchJob>(
            responseBody,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
        );

        Assert.NotNull(searchJob);
        Assert.Equal(expectedJobId, searchJob.JobId);
        Assert.Equal(SearchStatus.Queued, searchJob.Status);
    }

    [Fact]
    public async Task SearchesV2_ShouldEncryptPersonId_WhenEncryptionIsEnabled()
    {
        // Arrange
        _encryptionConfig.Value.Returns(
            new EncryptionConfiguration { EnablePersonIdEncryption = true }
        );

        var requestData = new StartSearchRequest("Cy13hyZL-4LSIwVy50p-Hg");
        var request = MockHttpRequestData.CreateJson(requestData);
        var context = CreateContextWithAuth("test-client");

        var searchJobDto = new SearchJobDto
        {
            JobId = Guid.NewGuid().ToString(),
            PersonId = "Cy13hyZL-4LSIwVy50p-Hg",
            Status = SearchStatus.Queued,
            CreatedAt = DateTime.UtcNow,
            LastUpdatedAt = DateTime.UtcNow,
        };

        _findQueueService
            .PostSearchJobAsync(Arg.Any<SearchRequestMessage>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(searchJobDto));

        // Act
        var result = await _function.SearchesV2(request, context, CancellationToken.None);

        // Assert
        Assert.Equal(HttpStatusCode.Accepted, result.StatusCode);

        await _findQueueService
            .Received(1)
            .PostSearchJobAsync(
                Arg.Is<SearchRequestMessage>(m =>
                    m.PersonId == "Cy13hyZL-4LSIwVy50p-Hg"
                    && m.PersonId.Length > 0
                    && m.RequestingCustodianId == "test-client"
                ),
                Arg.Any<CancellationToken>()
            );
    }

    private static FunctionContext CreateContextWithAuth(string clientId)
    {
        var context = Substitute.For<FunctionContext>();
        var authContext = new AuthContext(clientId, ["find-record.write"]);

        var items = new Dictionary<object, object>
        {
            { ApplicationConstants.Auth.AuthContextKey, authContext },
        };

        context.Items.Returns(items);
        context.TraceContext.Returns(Substitute.For<TraceContext>());
        context.InvocationId.Returns("test-invocation-id");

        return context;
    }
}
