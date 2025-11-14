using System.Net;
using System.Net.Http.Json;
using MediatR;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using SUI.FakeCustodians.Application.Common;
using SUI.FakeCustodians.Application.Models;
using SUI.FakeCustodians.Application.Queries;

namespace SUI.FakeCustodians.API.Unit.Tests;

public class EventsControllerApiTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly IMediator _mockMediator;
    private readonly HttpClient _client;

    public EventsControllerApiTests(WebApplicationFactory<Program> factory)
    {
        _mockMediator = Substitute.For<IMediator>();

        var appFactory = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                // Remove real mediator if registered
                var descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(IMediator));
                if (descriptor != null)
                    services.Remove(descriptor);

                // Add mock mediator
                services.AddSingleton(_mockMediator);
            });
        });

        _client = appFactory.CreateClient();
    }

    /// <summary>
    /// Success (200)
    /// </summary>
    [Fact]
    public async Task GetEventsBySui_ShouldReturnOk_WhenHandlerSucceeds()
    {
        var sui = "1234567890";
        var mockResponse = new EventResponse { Sui = sui };

        _mockMediator
            .Send(Arg.Any<GetEventRecordBySuiQuery>(), Arg.Any<CancellationToken>())
            .Returns(HandlerResult<EventResponse>.Success(mockResponse));

        var response = await _client.GetAsync($"/api/v1/events/{sui}");
        response.EnsureSuccessStatusCode();

        var content = await response.Content.ReadFromJsonAsync<EventResponse>();
        Assert.NotNull(content);
        Assert.Equal(sui, content.Sui);
    }

    /// <summary>
    /// Not Found (404)
    /// </summary>
    [Fact]
    public async Task GetEventsBySui_ShouldReturnNotFound_WhenHandlerReturnsNotFound()
    {
        _mockMediator
            .Send(Arg.Any<GetEventRecordBySuiQuery>(), Arg.Any<CancellationToken>())
            .Returns(HandlerResult<EventResponse>.NotFound("record missing"));

        var response = await _client.GetAsync("/api/v1/events/unknown-sui");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
