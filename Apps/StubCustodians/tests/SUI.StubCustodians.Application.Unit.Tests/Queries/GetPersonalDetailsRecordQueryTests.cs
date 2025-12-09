using NSubstitute;
using SUI.Custodians.Domain.Models;
using SUI.StubCustodians.Application.Common;
using SUI.StubCustodians.Application.Interfaces;
using SUI.StubCustodians.Application.Models;
using SUI.StubCustodians.Application.Queries;

namespace SUI.StubCustodians.Application.Unit.Tests.Queries
{
    public class GetPersonalDetailsRecordQueryHandlerTests
    {
        private readonly IRecordProvider<PersonalDetailsRecordV1> _recordProvider;
        private readonly GetPersonalDetailsRecordQueryHandler _handler;

        public GetPersonalDetailsRecordQueryHandlerTests()
        {
            _recordProvider = Substitute.For<IRecordProvider<PersonalDetailsRecordV1>>();
            _handler = new GetPersonalDetailsRecordQueryHandler(_recordProvider);
        }

        [Fact]
        public async Task Handle_InvalidSuiOrProvider_ReturnsValidationFailure()
        {
            var query = new GetPersonalDetailsRecordQuery
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

            var query = new GetPersonalDetailsRecordQuery
            {
                Sui = sui,
                ProviderSystemId = provider,
            };

            _recordProvider
                .GetRecordForSui(sui, provider)
                .Returns((RecordEnvelope<PersonalDetailsRecordV1>?)null);

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

            var envelope = new RecordEnvelope<PersonalDetailsRecordV1>
            {
                ProviderSystemId = provider,
                SchemaUri = new Uri("https://example.com"),
                Payload = new PersonalDetailsRecordV1(),
            };

            _recordProvider.GetRecordForSui(sui, provider).Returns(envelope);

            var query = new GetPersonalDetailsRecordQuery
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
