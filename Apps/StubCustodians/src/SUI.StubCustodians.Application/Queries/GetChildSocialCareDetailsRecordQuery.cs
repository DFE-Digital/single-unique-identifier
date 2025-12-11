using System.Diagnostics.CodeAnalysis;
using MediatR;
using SUI.StubCustodians.Application.Common;
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
        public Task<ResultType> Handle(
            GetChildSocialCareDetailsRecordQuery request,
            CancellationToken cancellationToken
        )
        {
            throw new NotImplementedException();
        }
    }
}
