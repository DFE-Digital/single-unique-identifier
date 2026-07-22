using System.Net;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using NSubstitute;
using SUI.GetAnIdentifier.Function.Functions;

namespace SUI.GetAnIdentifier.Function.Tests;

public class HelloWorldFunctionTests
{
    [Fact]
    public async Task HelloWorld_ReturnsOk_WithExpectedMessage()
    {
        // Arrange
        var workerOptions = new WorkerOptions
        {
            Serializer = new Azure.Core.Serialization.JsonObjectSerializer(),
        };

        var optionsSubstitute = Substitute.For<IOptions<WorkerOptions>>();
        optionsSubstitute.Value.Returns(workerOptions);

        var serviceCollection = new ServiceCollection();
        serviceCollection.AddSingleton(optionsSubstitute);
        var serviceProvider = serviceCollection.BuildServiceProvider();

        var contextSubstitute = Substitute.For<FunctionContext>();
        contextSubstitute.InstanceServices.Returns(serviceProvider);

        var responseSubstitute = Substitute.For<HttpResponseData>(contextSubstitute);
        var bodyStream = new MemoryStream();

        responseSubstitute.Body.Returns(bodyStream);
        responseSubstitute.Headers.Returns(new HttpHeadersCollection());

        var requestSubstitute = Substitute.For<HttpRequestData>(contextSubstitute);
        requestSubstitute.CreateResponse().Returns(responseSubstitute);

        var sut = new HelloWorldFunction();

        // Act
        var response = await sut.HelloWorld(requestSubstitute);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        bodyStream.Position = 0;
        using var reader = new StreamReader(bodyStream);
        var responseBody = await reader.ReadToEndAsync();

        Assert.Equal("\"Hello World\"", responseBody);
    }
}
