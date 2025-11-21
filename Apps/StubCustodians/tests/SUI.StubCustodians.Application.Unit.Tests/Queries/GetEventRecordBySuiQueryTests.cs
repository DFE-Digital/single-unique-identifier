using NSubstitute;
using SUI.StubCustodians.Application.Common;
using SUI.StubCustodians.Application.Interfaces;
using SUI.StubCustodians.Application.Models;
using SUI.StubCustodians.Application.Queries;

namespace SUI.StubCustodians.Application.Unit.Tests.Queries
{
    public class GetEventRecordBySuiQueryHandlerTests
    {
        private readonly IEventRecordProvider _eventRecordProvider;
        private readonly GetEventRecordBySuiQueryHandler _handler;

        public GetEventRecordBySuiQueryHandlerTests()
        {
            _eventRecordProvider = Substitute.For<IEventRecordProvider>();
            _handler = new GetEventRecordBySuiQueryHandler(_eventRecordProvider);
        }

        [Fact]
        public async Task Handle_InvalidSui_ReturnsValidationFailure()
        {
            var query = new GetEventRecordBySuiQuery { Sui = "invalid!" };

            var result = await _handler.Handle(query, CancellationToken.None);

            Assert.False(result.IsSuccess);
            Assert.Equal(FailureKind.Validation, result.Failure!.Kind);
            Assert.NotEmpty(result.Failure.Errors);
        }

        [Fact]
        public async Task Handle_ValidSui_NoRecordFound_ReturnsNotFound()
        {
            var sui = "1234567890";
            var query = new GetEventRecordBySuiQuery { Sui = sui };
            _eventRecordProvider.GetEventRecordForSui(sui).Returns((EventResponse?)null);

            var result = await _handler.Handle(query, CancellationToken.None);

            Assert.False(result.IsSuccess);
            Assert.Equal(FailureKind.NotFound, result.Failure!.Kind);
            Assert.Single(result.Failure.Errors);
            Assert.Contains(sui, result.Failure.Errors.First().Message);
        }

        [Fact]
        public async Task Handle_ValidSui_RecordFound_ReturnsSuccess()
        {
            var sui = "1234567890";
            var eventResponse = new EventResponse { Sui = sui };
            _eventRecordProvider.GetEventRecordForSui(sui).Returns(eventResponse);
            var query = new GetEventRecordBySuiQuery { Sui = sui };

            var result = await _handler.Handle(query, CancellationToken.None);

            Assert.True(result.IsSuccess);
            Assert.Equal(eventResponse, result.Result);
        }
    }
}
