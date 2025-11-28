using System.Net;
using System.Text.Json;
using Microsoft.Azure.Functions.Worker;
using Microsoft.DurableTask;
using Microsoft.DurableTask.Client;
using Microsoft.Extensions.Logging;
using NSubstitute;
using SUI.Find.Application.Constants;
using SUI.Find.FindApi.Functions.HttpTriggers;
using SUI.Find.FindApi.Functions.Orchestrators;
using SUI.Find.FindApi.Models;
using SUI.Find.FindApi.UnitTests.Mocks;

namespace SUI.Find.FindApi.UnitTests.FunctionTests;

public class SearchEndpointTests
{
    private readonly SearchFunction _sut;
    private readonly DurableTaskClient _client = Substitute.For<DurableTaskClient>("name");
    private readonly FunctionContext _context = Substitute.For<FunctionContext>();

    private const string ValidSuid = "1234567890123456"; 
    private const string InvalidSuid = "short";
    private const string TestClientId = "test-client-id";
    private const string InstanceId = $"{ValidSuid}-{TestClientId}";
    
    public SearchEndpointTests()
    {
        var logger = new LoggerFactory();
        var log = logger.CreateLogger<SearchFunction>();
        _sut = new SearchFunction(log);
        _context.InvocationId.Returns(Guid.NewGuid().ToString());
        var items = new Dictionary<object, object>
        {
            [ApplicationConstants.Auth.AuthContextKey] = new AuthContext(TestClientId, []),
        };
        _context.Items.Returns(items);
    }

    [Fact]
    public async Task ShouldReturn202_WhenRequestSuidIsValid()
    {
        // Arrange
        var data = new StartSearchRequest(ValidSuid);
        var httpRequestData = MockHttpRequestData.CreateJson(data);
        _client
            .ScheduleNewOrchestrationInstanceAsync(nameof(SearchOrchestrator), ValidSuid)
            .Returns("test-job-id");

        // Act
        var result = await _sut.Searches(httpRequestData, _client, _context);

        // Assert
        Assert.Equal(HttpStatusCode.Accepted, result.StatusCode);
    }

    [Fact]
    public async Task ShouldReturn202_WithSearchJobData_WhenSuccessful()
    {
        // Arrange
        var data = new StartSearchRequest(ValidSuid);
        var httpRequestData = MockHttpRequestData.CreateJson(data);
        _client.ScheduleNewOrchestrationInstanceAsync(
            Arg.Any<TaskName>(),                 
            Arg.Any<object>(),                   
            Arg.Any<StartOrchestrationOptions>() 
        ).Returns("job-id");

        // Act
        var result = await _sut.Searches(httpRequestData, _client, _context);

        // Assert
        result.Body.Position = 0;
        var responseData = await JsonSerializer.DeserializeAsync<SearchJob>(result.Body);
        Assert.NotNull(responseData);
        Assert.Equal(data.Suid, responseData.Suid);
        Assert.Equal(SearchStatus.Queued, responseData.Status);
        Assert.Equal("job-id", responseData.JobId);
        Assert.NotEmpty(responseData.Links);
    }

    [Fact]
    public async Task ShouldReturn202_WithLinks_WhenSuccessful()
    {
        // Arrange
        var data = new StartSearchRequest(ValidSuid);
        var httpRequestData = MockHttpRequestData.CreateJson(data);
        _client
            .ScheduleNewOrchestrationInstanceAsync("SearchOrchestrator", ValidSuid)
            .Returns("test-job-id");

        // Act
        var result = await _sut.Searches(httpRequestData, _client, _context);

        // Assert
        result.Body.Position = 0;
        var responseData = await JsonSerializer.DeserializeAsync<SearchJob>(result.Body);
        Assert.NotNull(responseData);
        Assert.True(responseData.Links.ContainsKey("self"));
        Assert.True(responseData.Links.ContainsKey("status"));
        Assert.True(responseData.Links.ContainsKey("cancel"));
    }

    [Fact]
    public async Task ShouldReturn400_WhenRequestSuidIsInValid()
    {
        // Arrange
        var data = new StartSearchRequest(InvalidSuid);
        var httpRequestData = MockHttpRequestData.CreateJson(data);

        // Act
        var result = await _sut.Searches(httpRequestData, _client, _context);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, result.StatusCode);

        result.Body.Position = 0;
        var responseData = await JsonSerializer.DeserializeAsync<Problem>(result.Body);
        Assert.NotNull(responseData?.Title);
    }

    [Fact]
    public async Task ShouldReturn400_AndInstanceShouldContainCorrelationId_WhenRequestSuidIsInValid()
    {
        // Arrange
        _context.Items["AuthContext"] = new AuthContext(TestClientId, ["scope"]);
        
        var data = new StartSearchRequest(InvalidSuid);
        var httpRequestData = MockHttpRequestData.CreateJson(data);
        
        // Act
        var result = await _sut.Searches(httpRequestData, _client, _context);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, result.StatusCode);

        result.Body.Position = 0;
        var responseData = await JsonSerializer.DeserializeAsync<Problem>(result.Body);
        Assert.NotNull(responseData?.Instance);
        var guid = responseData.Instance?.Split(':').Last();
        Assert.True(Guid.TryParse(guid, out _));
    }

    [Fact]
    public async Task ShouldReturn202_ExistingJob_WhenJobIsRunning_And_RequestIsDuplicated()
    {
        // Arrange
        _context.Items["AuthContext"] = new AuthContext(TestClientId, ["scope"]);
        
        var data = new StartSearchRequest(ValidSuid);
        var httpRequestData = MockHttpRequestData.CreateJson(data);
        
        var mockOrchestrationMetadata = new OrchestrationMetadata(
            nameof(SearchOrchestrator), 
            InstanceId
        )
        {
            RuntimeStatus = OrchestrationRuntimeStatus.Running,
            CreatedAt = default,
            LastUpdatedAt = default,
        };

        _client.GetInstanceAsync(InstanceId).Returns(
            mockOrchestrationMetadata
        );
        
        _client
            .ScheduleNewOrchestrationInstanceAsync("SearchOrchestrator", ValidSuid, new StartOrchestrationOptions( InstanceId: InstanceId))
            .Returns(InstanceId);
 

        // Act
        var result = await _sut.Searches(httpRequestData, _client, _context);
        
        // Assert
        Assert.Equal(HttpStatusCode.Accepted, result.StatusCode);
        result.Body.Position = 0;
        var responseData = await JsonSerializer.DeserializeAsync<SearchJob>(result.Body);
        Assert.Equal(InstanceId, responseData?.JobId);
        Assert.Equal(SearchStatus.Running, responseData?.Status);
        
        await _client.DidNotReceive().ScheduleNewOrchestrationInstanceAsync(
            Arg.Any<TaskName>(), 
            Arg.Any<object>(), 
            Arg.Any<StartOrchestrationOptions>()
        );
    }
    
    [Fact]
    public async Task ShouldReturn202_ExistingJob_WhenJobIsQueued_And_RequestIsDuplicated()
    {
        // Arrange
        _context.Items["AuthContext"] = new AuthContext(TestClientId, ["scope"]);
        
        var data = new StartSearchRequest(ValidSuid);
        var httpRequestData = MockHttpRequestData.CreateJson(data);
        
        var mockOrchestrationMetadata = new OrchestrationMetadata(
            nameof(SearchOrchestrator), 
            InstanceId
        )
        {
            RuntimeStatus = OrchestrationRuntimeStatus.Pending,
            CreatedAt = default,
            LastUpdatedAt = default,
        };

        _client.GetInstanceAsync(InstanceId).Returns(
            mockOrchestrationMetadata
        );

        // Act
        var result = await _sut.Searches(httpRequestData, _client, _context);

        // Assert
        Assert.Equal(HttpStatusCode.Accepted, result.StatusCode);
        result.Body.Position = 0;
        var responseData = await JsonSerializer.DeserializeAsync<SearchJob>(result.Body);
        Assert.Equal(InstanceId, responseData?.JobId);
        Assert.Equal(SearchStatus.Queued, responseData?.Status);
        
        await _client.DidNotReceive().ScheduleNewOrchestrationInstanceAsync(
            Arg.Any<TaskName>(), 
            Arg.Any<object>(), 
            Arg.Any<StartOrchestrationOptions>()
        );
    }
    
    [Fact]
    public async Task ShouldReturn202_WithNewSearchJob_WhenPreviousJobIsCompleted()
    {
        // Arrange
        _context.Items["AuthContext"] = new AuthContext(TestClientId, ["scope"]);
        
        var data = new StartSearchRequest(ValidSuid);
        var httpRequestData = MockHttpRequestData.CreateJson(data);
        
        var mockOrchestrationMetadata = new OrchestrationMetadata(
            nameof(SearchOrchestrator), 
            InstanceId 
        )
        {
            RuntimeStatus = OrchestrationRuntimeStatus.Completed,
            CreatedAt = default,
            LastUpdatedAt = default,
        };

        _client.GetInstanceAsync(InstanceId).Returns(
            mockOrchestrationMetadata
        );
        
        _client.ScheduleNewOrchestrationInstanceAsync(
            Arg.Any<TaskName>(), 
            Arg.Any<object>(), 
            Arg.Any<StartOrchestrationOptions>()
        ).Returns(InstanceId);

        // Act
        var result = await _sut.Searches(httpRequestData, _client, _context);

        // Assert
        Assert.Equal(HttpStatusCode.Accepted, result.StatusCode);
        result.Body.Position = 0;
        var responseData = await JsonSerializer.DeserializeAsync<SearchJob>(result.Body);
        Assert.Equal(InstanceId, responseData?.JobId);
        Assert.Equal(ValidSuid, responseData?.Suid);
        Assert.Equal(SearchStatus.Queued, responseData?.Status);
        
        await _client.Received().ScheduleNewOrchestrationInstanceAsync(
            Arg.Any<TaskName>(), 
            Arg.Any<object>(), 
            Arg.Any<StartOrchestrationOptions>()
        );
    }
}
