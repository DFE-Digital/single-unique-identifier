using NSubstitute;
using SUI.Custodians.Domain.Models;
using SUI.StubCustodians.Application.Common;
using SUI.StubCustodians.Application.Interfaces;
using SUI.StubCustodians.Application.Models;
using SUI.StubCustodians.Application.Queries;

namespace SUI.StubCustodians.Application.Unit.Tests.Queries
{
    public class GetCrimeDataRecordQueryTests
    {
        private readonly IRecordProvider<CrimeDataRecordV1> _recordProvider;
        private readonly GetCrimeDataRecordQueryHandler _handler;

        public GetCrimeDataRecordQueryTests()
        {
            _recordProvider = Substitute.For<IRecordProvider<CrimeDataRecordV1>>();
            _handler = new GetCrimeDataRecordQueryHandler(_recordProvider);
        }

        [Fact]
        public async Task Handle_InvalidSuiOrProvider_ReturnsValidationFailure()
        {
            var query = new GetCrimeDataRecordQuery()
            {
                Sui = "abc123", // invalid (not digits)
                ProviderSystemId = "Unknown", // invalid provider
            };

            var result = await _handler.Handle(query, CancellationToken.None);

            Assert.False(result.IsSuccess);
            Assert.Equal(FailureKind.Validation, result.Failure!.Kind);
            Assert.NotEmpty(result.Failure.Errors);
        }

        [Fact]
        public async Task Handle_ValidRequest_NoRecordFound_ReturnsNotFound()
        {
            var sui = "1234567890";
            var provider = "MockCrimeDataProvider";

            var query = new GetCrimeDataRecordQuery() { Sui = sui, ProviderSystemId = provider };

            _recordProvider
                .GetRecordForSui(sui, provider)
                .Returns((RecordEnvelope<CrimeDataRecordV1>?)null);

            var result = await _handler.Handle(query, CancellationToken.None);

            Assert.False(result.IsSuccess);
            Assert.Equal(FailureKind.NotFound, result.Failure!.Kind);
            Assert.Single(result.Failure.Errors);
            Assert.Contains(sui, result.Failure.Errors.First().Message);
        }

        [Fact]
        public async Task Handle_ValidRequest_RecordFound_ReturnsSuccess()
        {
            var sui = "1234567890";
            var provider = "MockCrimeDataProvider";

            var envelope = new RecordEnvelope<CrimeDataRecordV1>
            {
                SchemaUri = new Uri("https://example.com"),
                Payload = new CrimeDataRecordV1(),
            };

            _recordProvider.GetRecordForSui(sui, provider).Returns(envelope);

            var query = new GetCrimeDataRecordQuery() { Sui = sui, ProviderSystemId = provider };

            var result = await _handler.Handle(query, CancellationToken.None);

            Assert.True(result.IsSuccess);
            Assert.Equal(envelope, result.Result);
        }
    }
}
