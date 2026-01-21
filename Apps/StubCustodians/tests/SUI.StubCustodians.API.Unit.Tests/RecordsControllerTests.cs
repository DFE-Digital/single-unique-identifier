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

        private readonly IRecordServiceHandler<PersonalDetailsRecord> _mockPersonalHandler;
        private readonly IRecordServiceHandler<ChildrensServicesDetailsRecord> _mockChildrensHandler;
        private readonly IRecordServiceHandler<HealthDataRecord> _mockHealthHandler;
        private readonly IRecordServiceHandler<EducationDetailsRecord> _mockEducationHandler;
        private readonly IRecordServiceHandler<CrimeDataRecord> _mockCrimeHandler;

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

            var envelope = new RecordEnvelope<PersonalDetailsRecord>
            {
                Payload = new PersonalDetailsRecord(),
                SchemaUri = new Uri("https://example.com/schema"),
            };

            _mockPersonalHandler
                .GetRecord(sui, provider)
                .Returns(HandlerResult<RecordEnvelope<PersonalDetailsRecord>>.Success(envelope));

            var response = await _client.GetAsync(
                $"/api/v1/records/{provider}/PersonalDetailsRecord/{sui}"
            );

            var body = await response.Content.ReadFromJsonAsync<
                RecordEnvelope<PersonalDetailsRecord>
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
                .Returns(HandlerResult<RecordEnvelope<PersonalDetailsRecord>>.NotFound("missing"));

            var response = await _client.GetAsync(
                $"/api/v1/records/{provider}/PersonalDetailsRecord/{sui}"
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

            var envelope = new RecordEnvelope<ChildrensServicesDetailsRecord>
            {
                Payload = new ChildrensServicesDetailsRecord(),
                SchemaUri = new Uri("https://example.com/schema"),
            };

            _mockChildrensHandler
                .GetRecord(sui, provider)
                .Returns(
                    HandlerResult<RecordEnvelope<ChildrensServicesDetailsRecord>>.Success(envelope)
                );

            var response = await _client.GetAsync(
                $"/api/v1/records/{provider}/ChildrensServicesDetailsRecord/{sui}"
            );

            var body = await response.Content.ReadFromJsonAsync<
                RecordEnvelope<ChildrensServicesDetailsRecord>
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
                    HandlerResult<RecordEnvelope<ChildrensServicesDetailsRecord>>.NotFound(
                        "missing"
                    )
                );

            var response = await _client.GetAsync(
                $"/api/v1/records/{provider}/ChildrensServicesDetailsRecord/{sui}"
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
                .Returns(HandlerResult<RecordEnvelope<EducationDetailsRecord>>.Success(envelope));

            var response = await _client.GetAsync(
                $"/api/v1/records/{provider}/EducationDetailsRecord/{sui}"
            );

            var body = await response.Content.ReadFromJsonAsync<
                RecordEnvelope<EducationDetailsRecord>
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
                .Returns(HandlerResult<RecordEnvelope<EducationDetailsRecord>>.NotFound("missing"));

            var response = await _client.GetAsync(
                $"/api/v1/records/{provider}/EducationDetailsRecord/{sui}"
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

            var envelope = new RecordEnvelope<HealthDataRecord>
            {
                Payload = new HealthDataRecord(),
                SchemaUri = new Uri("https://example.com/schema"),
            };

            _mockHealthHandler
                .GetRecord(sui, provider)
                .Returns(HandlerResult<RecordEnvelope<HealthDataRecord>>.Success(envelope));

            var response = await _client.GetAsync(
                $"/api/v1/records/{provider}/HealthDataRecord/{sui}"
            );

            var body = await response.Content.ReadFromJsonAsync<RecordEnvelope<HealthDataRecord>>();

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
                .Returns(HandlerResult<RecordEnvelope<HealthDataRecord>>.NotFound("missing"));

            var response = await _client.GetAsync(
                $"/api/v1/records/{provider}/HealthDataRecord/{sui}"
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

            var envelope = new RecordEnvelope<CrimeDataRecord>
            {
                Payload = new CrimeDataRecord(),
                SchemaUri = new Uri("https://example.com/schema"),
            };

            _mockCrimeHandler
                .GetRecord(sui, provider)
                .Returns(HandlerResult<RecordEnvelope<CrimeDataRecord>>.Success(envelope));

            var response = await _client.GetAsync(
                $"/api/v1/records/{provider}/CrimeDataRecord/{sui}"
            );

            var body = await response.Content.ReadFromJsonAsync<RecordEnvelope<CrimeDataRecord>>();

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
                .Returns(HandlerResult<RecordEnvelope<CrimeDataRecord>>.NotFound("missing"));

            var response = await _client.GetAsync(
                $"/api/v1/records/{provider}/CrimeDataRecord/{sui}"
            );

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        #endregion
    }
}
