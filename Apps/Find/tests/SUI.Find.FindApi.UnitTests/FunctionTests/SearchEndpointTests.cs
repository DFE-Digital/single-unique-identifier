using System.Net;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using NSubstitute;
using SUI.Find.FindApi.Functions;

namespace SUI.Find.FindApi.UnitTests;

public class SearchEndpointTests
{
  private readonly SearchFunction Sut;

  public SearchEndpointTests()
  {
    var logger = new LoggerFactory();
    ILogger<SearchFunction> log = logger.CreateLogger<SearchFunction>();
    Sut = new SearchFunction(log);
  }

  [Fact]
  public async Task ShouldReturn202_WhenRequestSuidIsValid()
  {
    // Arrange
    var httpRequestData = await SetupRequestPostData("{\"Suid\":\"9434765870\"}");

    // Act
    var result = await Sut.Searches(httpRequestData);

    // Assert
    Assert.Equal(HttpStatusCode.Accepted, result);
  }

  [Fact]
  public async Task ShouldReturn400_WhenRequestSuidIsInValid()
  {
    // Arrange
    var httpRequestData = await SetupRequestPostData("{\"Suid\":\"test-suid-bad-123\"}");

    // Act
    var result = await Sut.Searches(httpRequestData);

    // Assert

    Assert.Equal(HttpStatusCode.BadRequest, result);
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
    return httpRequestData;

  }

}
