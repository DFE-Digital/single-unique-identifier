using NSubstitute;
using SUI.Custodians.Domain.Models;
using SUI.StubCustodians.Application.Common;
using SUI.StubCustodians.Application.Interfaces;
using SUI.StubCustodians.Application.Models;
using SUI.StubCustodians.Application.Queries;

namespace SUI.StubCustodians.Application.Unit.Tests.Queries
{
    public class GetEducationDetailsRecordQueryHandlerTests
    {
        private readonly IRecordProvider<EducationDetailsRecordV1> _recordProvider;
        private readonly GetEducationDetailsRecordQueryHandler _handler;

        public GetEducationDetailsRecordQueryHandlerTests()
        {
            _recordProvider = Substitute.For<IRecordProvider<EducationDetailsRecordV1>>();
            _handler = new GetEducationDetailsRecordQueryHandler(_recordProvider);
        }

        [Fact]
        public async Task Handle_InvalidSuiOrProvider_ReturnsValidationFailure()
        {
            var query = new GetEducationDetailsRecordQuery
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
            var provider = "MockEducationProvider";

            var query = new GetEducationDetailsRecordQuery
            {
                Sui = sui,
                ProviderSystemId = provider,
            };

            _recordProvider
                .GetRecordForSui(sui, provider)
                .Returns((RecordEnvelope<EducationDetailsRecordV1>?)null);

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

            var envelope = new RecordEnvelope<EducationDetailsRecordV1>
            {
                SchemaUri = new Uri("https://example.com"),
                Payload = new EducationDetailsRecordV1(),
            };

            _recordProvider.GetRecordForSui(sui, provider).Returns(envelope);

            var query = new GetEducationDetailsRecordQuery
            {
                Sui = sui,
                ProviderSystemId = provider,
            };

            var result = await _handler.Handle(query, CancellationToken.None);

            Assert.True(result.IsSuccess);
            Assert.Equal(envelope, result.Result);
        }
    }
}
