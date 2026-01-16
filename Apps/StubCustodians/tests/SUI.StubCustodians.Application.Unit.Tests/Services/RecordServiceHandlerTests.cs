#nullable disable

using Microsoft.Extensions.Logging;
using NSubstitute;
using SUI.Custodians.Domain.Models;
using SUI.StubCustodians.Application.Interfaces;
using SUI.StubCustodians.Application.Models;
using SUI.StubCustodians.Application.Services;

namespace SUI.StubCustodians.Application.Unit.Tests.Services
{
    public class RecordServiceHandlerTests
    {
        private readonly IRecordProvider<PersonalDetailsRecordV1> _mockProvider;
        private readonly ILogger<RecordServiceHandler<PersonalDetailsRecordV1>> _mockLogger;
        private readonly RecordServiceHandler<PersonalDetailsRecordV1> _service;

        public RecordServiceHandlerTests()
        {
            _mockProvider = Substitute.For<IRecordProvider<PersonalDetailsRecordV1>>();
            _mockLogger = Substitute.For<ILogger<RecordServiceHandler<PersonalDetailsRecordV1>>>();
            _service = new RecordServiceHandler<PersonalDetailsRecordV1>(
                _mockProvider,
                _mockLogger
            );
        }

        [Fact]
        public async Task GetRecord_ValidSuiAndProvider_ReturnsSuccess()
        {
            const string sui = "1234567890";
            const string provider = "MockCrimeDataProvider";

            var record = new PersonalDetailsRecordV1();
            var envelope = new RecordEnvelope<PersonalDetailsRecordV1>
            {
                Payload = record,
                SchemaUri = new Uri("https://example.com/schema"),
            };

            _mockProvider.GetRecordForSui(sui, provider).Returns(envelope);

            var result = await _service.GetRecord(sui, provider);

            Assert.True(result.IsSuccess);
            Assert.Equal(envelope, result.Result);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public async Task GetRecord_InvalidSui_ReturnsValidationFailure(string sui)
        {
            const string provider = "MockCrimeDataProvider";

            var result = await _service.GetRecord(sui, provider);

            Assert.False(result.IsSuccess);
            Assert.True(result.Failure.Errors.Count > 0);
            Assert.Contains(result.Failure.Errors, e => e.Scope == "sui");
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData("InvalidProvider")]
        public async Task GetRecord_InvalidProvider_ReturnsValidationFailure(string provider)
        {
            const string sui = "1234567890";

            var result = await _service.GetRecord(sui, provider);

            Assert.False(result.IsSuccess);
            Assert.True(result.Failure.Errors.Count > 0);
            Assert.Contains(result.Failure.Errors, e => e.Scope == "providerSystemId");
        }

        [Fact]
        public async Task GetRecord_RecordNotFound_ReturnsNotFound()
        {
            const string sui = "1234567890";
            const string provider = "MockCrimeDataProvider";

            _mockProvider
                .GetRecordForSui(sui, provider)
                .Returns((RecordEnvelope<PersonalDetailsRecordV1>)null);

            var result = await _service.GetRecord(sui, provider);

            Assert.False(result.IsSuccess);
            Assert.True(result.Failure.Errors.Count > 0);
            Assert.Null(result.Result);
            Assert.Contains("not found", result.Failure.Errors.First().Message);
        }
    }
}
