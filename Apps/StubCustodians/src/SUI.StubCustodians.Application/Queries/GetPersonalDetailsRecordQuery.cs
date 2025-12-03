using System.Diagnostics.CodeAnalysis;
using MediatR;
using SUI.StubCustodians.Application.Common;
using ResultType = SUI.StubCustodians.Application.Common.HandlerResult<SUI.StubCustodians.Application.Models.RecordEnvelope<SUI.Custodians.Domain.Models.PersonalDetailsRecordV1>>;

namespace SUI.StubCustodians.Application.Queries
{
    [ExcludeFromCodeCoverage]
    public record GetPersonalDetailsRecordQuery : GetRecordQueryBase, IRequest<ResultType>
    {
        public IReadOnlyCollection<ErrorInfo> ValidateCommand() => ValidateCommon();
    }

    [ExcludeFromCodeCoverage]
    public class GetPersonalDetailsRecordQueryHandler
        : IRequestHandler<GetPersonalDetailsRecordQuery, ResultType>
    {
        public Task<ResultType> Handle(
            GetPersonalDetailsRecordQuery request,
            CancellationToken cancellationToken
        )
        {
            throw new NotImplementedException();
        }
    }
}
