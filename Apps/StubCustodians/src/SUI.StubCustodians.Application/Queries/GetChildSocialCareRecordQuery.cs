using System.Diagnostics.CodeAnalysis;
using MediatR;
using SUI.StubCustodians.Application.Common;
using ResultType = SUI.StubCustodians.Application.Common.HandlerResult<SUI.StubCustodians.Application.Models.RecordEnvelope<SUI.Custodians.Domain.Models.ChildSocialCareDetailsRecordV1>>;

namespace SUI.StubCustodians.Application.Queries
{
    [ExcludeFromCodeCoverage]
    public record GetChildSocialCareRecordQuery : GetRecordQueryBase, IRequest<ResultType>
    {
        public IReadOnlyCollection<ErrorInfo> ValidateCommand() => ValidateCommon();
    }

    [ExcludeFromCodeCoverage]
    public class GetChildSocialCareRecordQueryHandler
        : IRequestHandler<GetChildSocialCareRecordQuery, ResultType>
    {
        public Task<ResultType> Handle(
            GetChildSocialCareRecordQuery request,
            CancellationToken cancellationToken
        )
        {
            throw new NotImplementedException();
        }
    }
}
