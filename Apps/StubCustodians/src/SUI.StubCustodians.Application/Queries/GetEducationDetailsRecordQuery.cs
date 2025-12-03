using System.Diagnostics.CodeAnalysis;
using MediatR;
using SUI.StubCustodians.Application.Common;
using ResultType = SUI.StubCustodians.Application.Common.HandlerResult<SUI.StubCustodians.Application.Models.RecordEnvelope<SUI.Custodians.Domain.Models.EducationDetailsRecordV1>>;

namespace SUI.StubCustodians.Application.Queries
{
    [ExcludeFromCodeCoverage]
    public record GetEducationDetailsRecordQuery : GetRecordQueryBase, IRequest<ResultType>
    {
        public IReadOnlyCollection<ErrorInfo> ValidateCommand() => ValidateCommon();
    }

    [ExcludeFromCodeCoverage]
    public class GetEducationDetailsRecordQueryHandler
        : IRequestHandler<GetEducationDetailsRecordQuery, ResultType>
    {
        public Task<ResultType> Handle(
            GetEducationDetailsRecordQuery request,
            CancellationToken cancellationToken
        )
        {
            throw new NotImplementedException();
        }
    }
}
