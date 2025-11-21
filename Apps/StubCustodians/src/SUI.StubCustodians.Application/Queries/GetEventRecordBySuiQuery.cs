using MediatR;
using SUI.StubCustodians.Application.Common;
using SUI.StubCustodians.Application.Interfaces;
using ResultType = SUI.StubCustodians.Application.Common.HandlerResult<SUI.StubCustodians.Application.Models.EventResponse>;

namespace SUI.StubCustodians.Application.Queries
{
    public record GetEventRecordBySuiQuery : IRequest<ResultType>
    {
        public required string Sui { get; init; }

        /// <summary>
        /// Validation is temporarily based on the NHS number being the SUI.
        /// Ideally NHS number should be validated against the NHS number system.
        /// </summary>
        public IReadOnlyCollection<ErrorInfo> ValidateCommand()
        {
            var errors = new List<ErrorInfo>();

            if (string.IsNullOrWhiteSpace(Sui))
            {
                errors.Add(new ErrorInfo(nameof(Sui), $"Value cannot be null or whitespace."));
            }

            if (!Sui.All(char.IsDigit))
            {
                errors.Add(new ErrorInfo(nameof(Sui), $"Value can only contain digits."));
            }

            if (Sui.Length != 10)
            {
                errors.Add(new ErrorInfo(nameof(Sui), $"Value must have 10 digits only."));
            }

            return errors;
        }
    }

    public class GetEventRecordBySuiQueryHandler
        : IRequestHandler<GetEventRecordBySuiQuery, ResultType>
    {
        private readonly IEventRecordProvider _eventRecordProvider;

        public GetEventRecordBySuiQueryHandler(IEventRecordProvider eventRecordProvider)
        {
            _eventRecordProvider = eventRecordProvider;
        }

        public Task<ResultType> Handle(
            GetEventRecordBySuiQuery request,
            CancellationToken cancellationToken
        )
        {
            var errors = request.ValidateCommand();

            if (errors.Count > 0)
            {
                return Task.FromResult(ResultType.ValidationFailure(errors));
            }

            var result = _eventRecordProvider.GetEventRecordForSui(request.Sui);

            if (result == null)
            {
                return Task.FromResult(
                    ResultType.NotFound($"EventRecords for SUI:'{request.Sui}' not found")
                );
            }

            return Task.FromResult(ResultType.Success(result));
        }
    }
}
