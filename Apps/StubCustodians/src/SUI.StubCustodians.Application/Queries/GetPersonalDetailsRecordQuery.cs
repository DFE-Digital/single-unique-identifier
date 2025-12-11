using MediatR;
using SUI.Custodians.Domain.Models;
using SUI.StubCustodians.Application.Common;
using SUI.StubCustodians.Application.Interfaces;
using ResultType = SUI.StubCustodians.Application.Common.HandlerResult<SUI.StubCustodians.Application.Models.RecordEnvelope<SUI.Custodians.Domain.Models.PersonalDetailsRecordV1>>;

namespace SUI.StubCustodians.Application.Queries
{
    public record GetPersonalDetailsRecordQuery : GetRecordQueryBase, IRequest<ResultType>
    {
        public IReadOnlyCollection<ErrorInfo> ValidateCommand() => ValidateCommon();
    }

    public class GetPersonalDetailsRecordQueryHandler
        : IRequestHandler<GetPersonalDetailsRecordQuery, ResultType>
    {
        private readonly IRecordProvider<PersonalDetailsRecordV1> _recordProvider;

        public GetPersonalDetailsRecordQueryHandler(
            IRecordProvider<PersonalDetailsRecordV1> recordProvider
        )
        {
            _recordProvider = recordProvider;
        }

        public Task<ResultType> Handle(
            GetPersonalDetailsRecordQuery request,
            CancellationToken cancellationToken
        )
        {
            var errors = request.ValidateCommand();

            if (errors.Count > 0)
            {
                return Task.FromResult(ResultType.ValidationFailure(errors));
            }

            var result = _recordProvider.GetRecordForSui(request.Sui, request.ProviderSystemId);

            if (result == null)
            {
                return Task.FromResult(
                    ResultType.NotFound(
                        $"Records of type {typeof(PersonalDetailsRecordV1)} for SUI:'{request.Sui}' not found"
                    )
                );
            }

            return Task.FromResult(ResultType.Success(result));
        }
    }
}
