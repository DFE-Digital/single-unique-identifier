using System.Net;
using System.Text.Json;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using NSubstitute;
using SUI.Find.FindApi.Functions;
using SUI.Find.FindApi.Models;
using SUI.Find.FindApi.UnitTests.Mocks;

namespace SUI.Find.FindApi.UnitTests.FunctionTests;

public class SearchEndpointTests
{
    private readonly SearchFunction _sut;

    public SearchEndpointTests()
    {
        var logger = new LoggerFactory();
        ILogger<SearchFunction> log = logger.CreateLogger<SearchFunction>();
        _sut = new SearchFunction(log);
    }

    [Fact]
    public async Task ShouldReturn202_WhenRequestSuidIsValid()
    {
        // Arrange
        var data = new StartSearchRequest("9434765870");
        var httpRequestData = MockHttpRequestData.Create(data);

        // Act
        var result = await _sut.Searches(httpRequestData);

        // Assert
        Assert.Equal(HttpStatusCode.Accepted, result.StatusCode);
    }

    [Fact]
    public async Task ShouldReturn400_WhenRequestSuidIsInValid()
    {
        // Arrange
        var data = new StartSearchRequest("1234567890");
        var httpRequestData = MockHttpRequestData.Create(data);

        // Act
        var result = await _sut.Searches(httpRequestData);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, result.StatusCode);

        result.Body.Position = 0;
        var responseData = await JsonSerializer.DeserializeAsync<Problem>(result.Body);
        Assert.NotNull(responseData?.Title); // Simple check to show we are returning a problem details object
    }

    [Fact]
    public async Task ShouldReturn500_WhenRequestThereIsAServerError_DoesNotExposeInternalMessage()
    {
        // Arrange

        // Act

        // Assert

        throw new NotImplementedException();
    }

    private async Task<HttpRequestData> SetupRequestPostData(string dataToWrite)
    {
        var functionContext = Substitute.For<FunctionContext>();
        var httpRequestData = Substitute.For<HttpRequestData>(functionContext);
        var requestBody = new MemoryStream();
        var writer = new StreamWriter(requestBody, leaveOpen: true);
        await writer.WriteAsync(dataToWrite);
        await writer.FlushAsync();
        requestBody.Position = 0;

        httpRequestData.Method.Returns("POST");
        httpRequestData.Body.Returns(requestBody);
        var functionContext2 = httpRequestData.FunctionContext;
        var httpResponseData = Substitute.For<HttpResponseData>(functionContext2);
        httpRequestData.CreateResponse(HttpStatusCode.Accepted).Returns(httpResponseData);
        return httpRequestData;
    }
}
