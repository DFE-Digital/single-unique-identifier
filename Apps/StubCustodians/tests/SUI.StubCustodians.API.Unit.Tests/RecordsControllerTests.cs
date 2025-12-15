using System.Net;
using System.Net.Http.Json;
using MediatR;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using SUI.Custodians.Domain.Models;
using SUI.StubCustodians.Application.Common;
using SUI.StubCustodians.Application.Models;
using SUI.StubCustodians.Application.Queries;

namespace SUI.StubCustodians.API.Unit.Tests
{
    public class RecordsControllerApiTests : IClassFixture<WebApplicationFactory<Program>>
    {
        private readonly IMediator _mockMediator;
        private readonly HttpClient _client;

        public RecordsControllerApiTests(WebApplicationFactory<Program> factory)
        {
            _mockMediator = Substitute.For<IMediator>();

            var appFactory = factory.WithWebHostBuilder(builder =>
            {
                builder.ConfigureServices(services =>
                {
                    // Replace real mediator with mock
                    var descriptor = services.SingleOrDefault(s =>
                        s.ServiceType == typeof(IMediator)
                    );
                    if (descriptor != null)
                        services.Remove(descriptor);

                    services.AddSingleton(_mockMediator);
                });
            });

            _client = appFactory.CreateClient();
        }

        /// <summary>
        /// PersonalDetailsRecords
        /// </summary>
        [Fact]
        public async Task GetPersonalDetailsRecord_ShouldReturnOk_WhenHandlerSucceeds()
        {
            var sui = "12345";
            var provider = "PROVIDER-A";

            var envelope = new RecordEnvelope<PersonalDetailsRecordV1>
            {
                Payload = new PersonalDetailsRecordV1(),
                SchemaUri = new Uri("https://example.com/schema"),
            };

            _mockMediator
                .Send(Arg.Any<GetPersonalDetailsRecordQuery>(), Arg.Any<CancellationToken>())
                .Returns(HandlerResult<RecordEnvelope<PersonalDetailsRecordV1>>.Success(envelope));

            var response = await _client.GetAsync(
                $"/api/v1/records/{provider}/PersonalDetailsRecordV1/{sui}"
            );

            var body = await response.Content.ReadFromJsonAsync<
                RecordEnvelope<PersonalDetailsRecordV1>
            >();

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.NotNull(body);
        }

        [Fact]
        public async Task GetPersonalDetailsRecord_ShouldReturnNotFound_WhenHandlerReturnsNotFound()
        {
            _mockMediator
                .Send(Arg.Any<GetPersonalDetailsRecordQuery>(), Arg.Any<CancellationToken>())
                .Returns(
                    HandlerResult<RecordEnvelope<PersonalDetailsRecordV1>>.NotFound("missing")
                );

            var response = await _client.GetAsync(
                "/api/v1/records/PROVIDER/PersonalDetailsRecordV1/UNKNOWN"
            );

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        /// <summary>
        /// ChildSocialCareDetailsRecords
        /// </summary>
        [Fact]
        public async Task GetChildSocialCareDetailsRecord_ShouldReturnOk_WhenHandlerSucceeds()
        {
            var sui = "23456";
            var provider = "PROVIDER-B";

            var envelope = new RecordEnvelope<ChildSocialCareDetailsRecordV1>
            {
                Payload = new ChildSocialCareDetailsRecordV1(),
                SchemaUri = new Uri("https://example.com/schema"),
            };

            _mockMediator
                .Send(Arg.Any<GetChildSocialCareDetailsRecordQuery>(), Arg.Any<CancellationToken>())
                .Returns(
                    HandlerResult<RecordEnvelope<ChildSocialCareDetailsRecordV1>>.Success(envelope)
                );

            var response = await _client.GetAsync(
                $"/api/v1/records/{provider}/ChildSocialCareDetailsRecordV1/{sui}"
            );

            var body = await response.Content.ReadFromJsonAsync<
                RecordEnvelope<ChildSocialCareDetailsRecordV1>
            >();

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.NotNull(body);
        }

        [Fact]
        public async Task GetChildSocialCareDetailsRecord_ShouldReturnNotFound_WhenHandlerReturnsNotFound()
        {
            _mockMediator
                .Send(Arg.Any<GetChildSocialCareDetailsRecordQuery>(), Arg.Any<CancellationToken>())
                .Returns(
                    HandlerResult<RecordEnvelope<ChildSocialCareDetailsRecordV1>>.NotFound(
                        "missing"
                    )
                );

            var response = await _client.GetAsync(
                "/api/v1/records/P/ChildSocialCareDetailsRecordV1/NOPE"
            );

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        /// <summary>
        /// EducationDetailsRecords
        /// </summary>
        [Fact]
        public async Task GetEducationDetailsRecord_ShouldReturnOk_WhenHandlerSucceeds()
        {
            var sui = "34567";
            var provider = "PROVIDER-C";

            var envelope = new RecordEnvelope<EducationDetailsRecordV1>
            {
                Payload = new EducationDetailsRecordV1(),
                SchemaUri = new Uri("https://example.com/schema"),
            };

            _mockMediator
                .Send(Arg.Any<GetEducationDetailsRecordQuery>(), Arg.Any<CancellationToken>())
                .Returns(HandlerResult<RecordEnvelope<EducationDetailsRecordV1>>.Success(envelope));

            var response = await _client.GetAsync(
                $"/api/v1/records/{provider}/EducationDetailsRecordV1/{sui}"
            );

            var body = await response.Content.ReadFromJsonAsync<
                RecordEnvelope<EducationDetailsRecordV1>
            >();

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.NotNull(body);
        }

        [Fact]
        public async Task GetEducationDetailsRecord_ShouldReturnNotFound_WhenHandlerReturnsNotFound()
        {
            _mockMediator
                .Send(Arg.Any<GetEducationDetailsRecordQuery>(), Arg.Any<CancellationToken>())
                .Returns(
                    HandlerResult<RecordEnvelope<EducationDetailsRecordV1>>.NotFound("missing")
                );

            var response = await _client.GetAsync(
                "/api/v1/records/X/EducationDetailsRecordV1/NONE"
            );

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        /// <summary>
        /// HealthDataRecords
        /// </summary>
        [Fact]
        public async Task GetHealthDataRecord_ShouldReturnOk_WhenHandlerSucceeds()
        {
            var sui = "45678";
            var provider = "PROVIDER-D";

            var envelope = new RecordEnvelope<HealthDataRecordV1>
            {
                Payload = new HealthDataRecordV1(),
                SchemaUri = new Uri("https://example.com/schema"),
            };

            _mockMediator
                .Send(Arg.Any<GetHealthDataRecordQuery>(), Arg.Any<CancellationToken>())
                .Returns(HandlerResult<RecordEnvelope<HealthDataRecordV1>>.Success(envelope));

            var response = await _client.GetAsync(
                $"/api/v1/records/{provider}/HealthDataRecordV1/{sui}"
            );

            var body = await response.Content.ReadFromJsonAsync<
                RecordEnvelope<HealthDataRecordV1>
            >();

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.NotNull(body);
        }

        [Fact]
        public async Task GetHealthDataRecord_ShouldReturnNotFound_WhenHandlerReturnsNotFound()
        {
            _mockMediator
                .Send(Arg.Any<GetHealthDataRecordQuery>(), Arg.Any<CancellationToken>())
                .Returns(HandlerResult<RecordEnvelope<HealthDataRecordV1>>.NotFound("missing"));

            var response = await _client.GetAsync("/api/v1/records/P/HealthDataRecordV1/NOPE");

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        /// <summary>
        /// CrimeDataRecords
        /// </summary>
        [Fact]
        public async Task GetCrimeDataRecord_ShouldReturnOk_WhenHandlerSucceeds()
        {
            var sui = "56789";
            var provider = "PROVIDER-E";

            var envelope = new RecordEnvelope<CrimeDataRecordV1>
            {
                Payload = new CrimeDataRecordV1(),
                SchemaUri = new Uri("https://example.com/schema"),
            };

            _mockMediator
                .Send(Arg.Any<GetCrimeDataRecordQuery>(), Arg.Any<CancellationToken>())
                .Returns(HandlerResult<RecordEnvelope<CrimeDataRecordV1>>.Success(envelope));

            var response = await _client.GetAsync(
                $"/api/v1/records/{provider}/CrimeDataRecordV1/{sui}"
            );

            var body = await response.Content.ReadFromJsonAsync<
                RecordEnvelope<CrimeDataRecordV1>
            >();

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.NotNull(body);
        }

        [Fact]
        public async Task GetCrimeDataRecord_ShouldReturnNotFound_WhenHandlerReturnsNotFound()
        {
            _mockMediator
                .Send(Arg.Any<GetCrimeDataRecordQuery>(), Arg.Any<CancellationToken>())
                .Returns(HandlerResult<RecordEnvelope<CrimeDataRecordV1>>.NotFound("missing"));

            var response = await _client.GetAsync("/api/v1/records/Z/CrimeDataRecordV1/NONE");

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }
    }
}
