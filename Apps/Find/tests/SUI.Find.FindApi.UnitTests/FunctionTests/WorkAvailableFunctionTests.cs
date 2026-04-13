using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using NSubstitute;
using SUI.Find.Application.Constants;
using SUI.Find.Application.Interfaces;
using SUI.Find.Application.Models;
using SUI.Find.FindApi.Functions.HttpFunctions;
using SUI.Find.FindApi.Models;
using SUI.Find.FindApi.UnitTests.Mocks;
using Xunit;

namespace SUI.Find.FindApi.UnitTests.FunctionTests;

public class WorkAvailableFunctionTests
{
    private readonly ILogger<WorkAvailableFunction> _logger = Substitute.For<
        ILogger<WorkAvailableFunction>
    >();
    private readonly IJobClaimService _jobClaimService = Substitute.For<IJobClaimService>();
    private readonly WorkAvailableFunction _function;

    public WorkAvailableFunctionTests()
    {
        _function = new WorkAvailableFunction(_logger, _jobClaimService);
    }

    [Fact]
    public async Task WorkAvailable_ShouldReturnUnauthorized_WhenAuthContextIsMissing()
    {
        // Arrange
        var request = MockHttpRequestData.Create();
        var context = Substitute.For<FunctionContext>();

        // Act
        var result = await _function.WorkAvailable(request, context, CancellationToken.None);

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, result.StatusCode);
    }

    [Fact]
    public async Task WorkAvailable_ShouldReturnOk_WhenJobsAreAvailable()
    {
        // Arrange
        var request = MockHttpRequestData.Create();
        var context = CreateContextWithAuth("test-client");

        _jobClaimService
            .DoesCustodianHaveJobs("test-client", Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(true));

        // Act
        var result = await _function.WorkAvailable(request, context, CancellationToken.None);

        // Assert
        Assert.Equal(HttpStatusCode.OK, result.StatusCode);
        await _jobClaimService
            .Received(1)
            .DoesCustodianHaveJobs("test-client", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task WorkAvailable_ShouldReturnNoContent_WhenNoJobsAreAvailable()
    {
        // Arrange
        var request = MockHttpRequestData.Create();
        var context = CreateContextWithAuth("test-client");

        _jobClaimService
            .DoesCustodianHaveJobs("test-client", Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(false));

        // Act
        var result = await _function.WorkAvailable(request, context, CancellationToken.None);

        // Assert
        Assert.Equal(HttpStatusCode.NoContent, result.StatusCode);
        await _jobClaimService
            .Received(1)
            .DoesCustodianHaveJobs("test-client", Arg.Any<CancellationToken>());
    }

    private static FunctionContext CreateContextWithAuth(string clientId)
    {
        var context = Substitute.For<FunctionContext>();
        var authContext = new AuthContext(clientId, ["work-item.read"]);

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
