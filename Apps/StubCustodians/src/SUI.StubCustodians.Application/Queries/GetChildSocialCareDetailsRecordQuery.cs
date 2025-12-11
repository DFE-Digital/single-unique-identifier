using MediatR;
using SUI.Custodians.Domain.Models;
using SUI.StubCustodians.Application.Common;
using SUI.StubCustodians.Application.Interfaces;
using ResultType = SUI.StubCustodians.Application.Common.HandlerResult<SUI.StubCustodians.Application.Models.RecordEnvelope<SUI.Custodians.Domain.Models.ChildSocialCareDetailsRecordV1>>;

namespace SUI.StubCustodians.Application.Queries
{
    public record GetChildSocialCareDetailsRecordQuery : GetRecordQueryBase, IRequest<ResultType>
    {
        public IReadOnlyCollection<ErrorInfo> ValidateCommand() => ValidateCommon();
    }

    public class GetChildSocialCareDetailsRecordQueryHandler
        : IRequestHandler<GetChildSocialCareDetailsRecordQuery, ResultType>
    {
        private readonly IRecordProvider<ChildSocialCareDetailsRecordV1> _recordProvider;

        public GetChildSocialCareDetailsRecordQueryHandler(
            IRecordProvider<ChildSocialCareDetailsRecordV1> recordProvider
        )
        {
            _recordProvider = recordProvider;
        }

        public Task<ResultType> Handle(
            GetChildSocialCareDetailsRecordQuery request,
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
                        $"Records of type {typeof(ChildSocialCareDetailsRecordV1)} for SUI:'{request.Sui}' not found"
                    )
                );
            }

            return Task.FromResult(ResultType.Success(result));
        }
    }
}
