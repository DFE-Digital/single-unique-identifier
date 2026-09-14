using System.Collections.Concurrent;
using System.Net;
using Azure;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using SUI.GetAnIdentifier.API.Middleware;
using SUI.GetAnIdentifier.Infrastructure.Interfaces;
using SUI.GetAnIdentifier.Infrastructure.Models;

namespace SUI.GetAnIdentifier.API.UnitTests.Middleware;

public class AuditMiddlewareTests
{
    private readonly ILogger<AuditMiddleware> _mockLogger = Substitute.For<
        ILogger<AuditMiddleware>
    >();
    private readonly IAuditService _mockAuditService = Substitute.For<IAuditService>();
    private readonly TimeProvider _timeProvider = Substitute.For<TimeProvider>();
    private bool _nextExecuted;

    private static FunctionContext CreateMockFunctionContext()
    {
        var context = Substitute.For<FunctionContext>();
        var features = Substitute.For<IInvocationFeatures>();
        var requestFeature = Substitute.For<IHttpRequestDataFeature>();

        var requestData = Substitute.For<HttpRequestData>(context);
        var responseData = Substitute.For<HttpResponseData>(context);

        var requestHeaders = new HttpHeadersCollection();
        requestData.Headers.Returns(requestHeaders);

        var responseHeaders = new HttpHeadersCollection();
        responseData.Headers.Returns(responseHeaders);

        requestData.Url.Returns(new Uri("https://mock.gov.uk/api/v1/searches"));
        requestData.CreateResponse().Returns(responseData);
        responseData.Body.Returns(new MemoryStream());

        var invocationResult = Substitute.For<InvocationResult>();
        context.GetInvocationResult().Returns(invocationResult);

        requestFeature
            .GetHttpRequestDataAsync(context)
            .Returns(ValueTask.FromResult<HttpRequestData?>(requestData));
        features.Get<IHttpRequestDataFeature>().Returns(requestFeature);
        context.Features.Returns(features);

        var items = new ConcurrentDictionary<object, object>();
        context.Items.Returns(items);
        context.FunctionDefinition.Name.Returns("GetAnIdentifier");
        context.FunctionDefinition.EntryPoint.Returns(
            "SUI.GetAnIdentifier.API.Functions.GetAnIdentifierFunction.GetAnIdentifier"
        );

        var workerOptions = new WorkerOptions
        {
            Serializer = Azure.Core.Serialization.JsonObjectSerializer.Default,
        };
        var mockWorkerOptionsContainer = Substitute.For<IOptions<WorkerOptions>>();
        mockWorkerOptionsContainer.Value.Returns(workerOptions);

        var mockServiceProvider = Substitute.For<IServiceProvider>();
        mockServiceProvider
            .GetService(typeof(IOptions<WorkerOptions>))
            .Returns(mockWorkerOptionsContainer);
        context.InstanceServices.Returns(mockServiceProvider);

        return context;
    }

    private Task<FunctionContext> Next(FunctionContext context)
    {
        _nextExecuted = true;
        return Task.FromResult(context);
    }

    [Theory]
    [InlineData("RenderOAuth2Redirect")]
    [InlineData("RenderOpenApiDocument")]
    [InlineData("RenderSwaggerDocument")]
    [InlineData("RenderSwaggerUI")]
    public async Task TestInvoke_WithNonAuditedFunctions_SkipsMethod(string functionName)
    {
        // Arrange
        var context = CreateMockFunctionContext();
        context.FunctionDefinition.Name.Returns(functionName);
        var sut = new AuditMiddleware(_mockLogger, _mockAuditService, _timeProvider);

        // Act
        await sut.Invoke(context, Next);

        // Assert
        await _mockAuditService.DidNotReceive().SendAuditEventAsync(Arg.Any<AuditEvent>());
        Assert.True(_nextExecuted);
    }

    [Fact]
    public async Task TestInvoke_WithSuccessfulResponse_AuditsRequest()
    {
        // Arrange
        var context = CreateMockFunctionContext();
        var sut = new AuditMiddleware(_mockLogger, _mockAuditService, _timeProvider);

        // Act
        await sut.Invoke(context, Next);

        // Assert
        await _mockAuditService.Received(1).SendAuditEventAsync(Arg.Any<AuditEvent>()); // Unable to set substitute for context response, so only incoming audit is hit
        Assert.True(_nextExecuted);
    }

    [Fact]
    public async Task TestInvoke_WithAuditFailure_AuditsRequest()
    {
        // Arrange
        var context = CreateMockFunctionContext();
        _mockAuditService
            .SendAuditEventAsync(Arg.Any<AuditEvent>())
            .ThrowsAsync(new RequestFailedException(404, "Azure service not found"));
        var sut = new AuditMiddleware(_mockLogger, _mockAuditService, _timeProvider);

        // Act
        await sut.Invoke(context, Next);

        // Assert
        Assert.False(_nextExecuted);
        Assert.NotNull(context.GetInvocationResult().Value);

        var responseData = Assert.IsType<HttpResponseData>(
            context.GetInvocationResult().Value,
            exactMatch: false
        );

        Assert.Equal(HttpStatusCode.InternalServerError, responseData.StatusCode);
    }
}
