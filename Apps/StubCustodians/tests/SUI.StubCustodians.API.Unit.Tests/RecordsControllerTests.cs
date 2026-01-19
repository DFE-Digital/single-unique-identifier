using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using SUI.Custodians.Domain.Models;
using SUI.StubCustodians.Application.Common;
using SUI.StubCustodians.Application.Interfaces;
using SUI.StubCustodians.Application.Models;

namespace SUI.StubCustodians.API.Unit.Tests
{
    public class RecordsControllerTests : IClassFixture<WebApplicationFactory<Program>>
    {
        private readonly HttpClient _client;

        private readonly IRecordServiceHandler<PersonalDetailsRecordV1> _mockPersonalHandler;
        private readonly IRecordServiceHandler<ChildrensServicesDetailsRecordV1> _mockChildrensHandler;
        private readonly IRecordServiceHandler<HealthDataRecordV1> _mockHealthHandler;
        private readonly IRecordServiceHandler<EducationDetailsRecordV1> _mockEducationHandler;
        private readonly IRecordServiceHandler<CrimeDataRecordV1> _mockCrimeHandler;

        public RecordsControllerTests(WebApplicationFactory<Program> factory)
        {
            _mockPersonalHandler = Substitute.For<IRecordServiceHandler<PersonalDetailsRecordV1>>();
            _mockChildrensHandler = Substitute.For<
                IRecordServiceHandler<ChildrensServicesDetailsRecordV1>
            >();
            _mockHealthHandler = Substitute.For<IRecordServiceHandler<HealthDataRecordV1>>();
            _mockEducationHandler = Substitute.For<
                IRecordServiceHandler<EducationDetailsRecordV1>
            >();
            _mockCrimeHandler = Substitute.For<IRecordServiceHandler<CrimeDataRecordV1>>();

            var appFactory = factory.WithWebHostBuilder(builder =>
            {
                builder.ConfigureServices(services =>
                {
                    // Replace real services with mocks
                    services.AddSingleton(_mockPersonalHandler);
                    services.AddSingleton(_mockChildrensHandler);
                    services.AddSingleton(_mockHealthHandler);
                    services.AddSingleton(_mockEducationHandler);
                    services.AddSingleton(_mockCrimeHandler);
                });
            });

            _client = appFactory.CreateClient();
        }

        #region PersonalDetailsRecord Tests

        [Fact]
        public async Task GetPersonalDetailsRecord_ShouldReturnOk_WhenHandlerSucceeds()
        {
            var sui = "1234567890";
            var provider = "MockEducationProvider";

            var envelope = new RecordEnvelope<PersonalDetailsRecordV1>
            {
                Payload = new PersonalDetailsRecordV1(),
                SchemaUri = new Uri("https://example.com/schema"),
            };

            _mockPersonalHandler
                .GetRecord(sui, provider)
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
            var sui = "9999999999";
            var provider = "MockEducationProvider";

            _mockPersonalHandler
                .GetRecord(sui, provider)
                .Returns(
                    HandlerResult<RecordEnvelope<PersonalDetailsRecordV1>>.NotFound("missing")
                );

            var response = await _client.GetAsync(
                $"/api/v1/records/{provider}/PersonalDetailsRecordV1/{sui}"
            );

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        #endregion

        #region ChildrensServicesDetailsRecord Tests

        [Fact]
        public async Task GetChildrensServicesDetailsRecord_ShouldReturnOk_WhenHandlerSucceeds()
        {
            var sui = "2345678901";
            var provider = "MockSocialCareProvider";

            var envelope = new RecordEnvelope<ChildrensServicesDetailsRecordV1>
            {
                Payload = new ChildrensServicesDetailsRecordV1(),
                SchemaUri = new Uri("https://example.com/schema"),
            };

            _mockChildrensHandler
                .GetRecord(sui, provider)
                .Returns(
                    HandlerResult<RecordEnvelope<ChildrensServicesDetailsRecordV1>>.Success(
                        envelope
                    )
                );

            var response = await _client.GetAsync(
                $"/api/v1/records/{provider}/ChildrensServicesDetailsRecordV1/{sui}"
            );

            var body = await response.Content.ReadFromJsonAsync<
                RecordEnvelope<ChildrensServicesDetailsRecordV1>
            >();

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.NotNull(body);
        }

        [Fact]
        public async Task GetChildrensServicesDetailsRecord_ShouldReturnNotFound_WhenHandlerReturnsNotFound()
        {
            var sui = "8888888888";
            var provider = "MockSocialCareProvider";

            _mockChildrensHandler
                .GetRecord(sui, provider)
                .Returns(
                    HandlerResult<RecordEnvelope<ChildrensServicesDetailsRecordV1>>.NotFound(
                        "missing"
                    )
                );

            var response = await _client.GetAsync(
                $"/api/v1/records/{provider}/ChildrensServicesDetailsRecordV1/{sui}"
            );

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        #endregion

        #region EducationDetailsRecord Tests

        [Fact]
        public async Task GetEducationDetailsRecord_ShouldReturnOk_WhenHandlerSucceeds()
        {
            var sui = "3456789012";
            var provider = "MockEducationProvider";

            var envelope = new RecordEnvelope<EducationDetailsRecordV1>
            {
                Payload = new EducationDetailsRecordV1(),
                SchemaUri = new Uri("https://example.com/schema"),
            };

            _mockEducationHandler
                .GetRecord(sui, provider)
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
            var sui = "7777777777";
            var provider = "MockEducationProvider";

            _mockEducationHandler
                .GetRecord(sui, provider)
                .Returns(
                    HandlerResult<RecordEnvelope<EducationDetailsRecordV1>>.NotFound("missing")
                );

            var response = await _client.GetAsync(
                $"/api/v1/records/{provider}/EducationDetailsRecordV1/{sui}"
            );

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        #endregion

        #region HealthDataRecord Tests

        [Fact]
        public async Task GetHealthDataRecord_ShouldReturnOk_WhenHandlerSucceeds()
        {
            var sui = "4567890123";
            var provider = "MockHealthcareProvider";

            var envelope = new RecordEnvelope<HealthDataRecordV1>
            {
                Payload = new HealthDataRecordV1(),
                SchemaUri = new Uri("https://example.com/schema"),
            };

            _mockHealthHandler
                .GetRecord(sui, provider)
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
            var sui = "6666666666";
            var provider = "MockHealthcareProvider";

            _mockHealthHandler
                .GetRecord(sui, provider)
                .Returns(HandlerResult<RecordEnvelope<HealthDataRecordV1>>.NotFound("missing"));

            var response = await _client.GetAsync(
                $"/api/v1/records/{provider}/HealthDataRecordV1/{sui}"
            );

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        #endregion

        #region CrimeDataRecord Tests

        [Fact]
        public async Task GetCrimeDataRecord_ShouldReturnOk_WhenHandlerSucceeds()
        {
            var sui = "5678901234";
            var provider = "MockCrimeDataProvider";

            var envelope = new RecordEnvelope<CrimeDataRecordV1>
            {
                Payload = new CrimeDataRecordV1(),
                SchemaUri = new Uri("https://example.com/schema"),
            };

            _mockCrimeHandler
                .GetRecord(sui, provider)
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
            var sui = "5555555555";
            var provider = "MockCrimeDataProvider";

            _mockCrimeHandler
                .GetRecord(sui, provider)
                .Returns(HandlerResult<RecordEnvelope<CrimeDataRecordV1>>.NotFound("missing"));

            var response = await _client.GetAsync(
                $"/api/v1/records/{provider}/CrimeDataRecordV1/{sui}"
            );

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        #endregion
    }
}
