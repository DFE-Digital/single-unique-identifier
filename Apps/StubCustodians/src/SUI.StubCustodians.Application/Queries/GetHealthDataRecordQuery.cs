using System.Diagnostics.CodeAnalysis;
using MediatR;
using SUI.StubCustodians.Application.Common;
using ResultType = SUI.StubCustodians.Application.Common.HandlerResult<SUI.StubCustodians.Application.Models.RecordEnvelope<SUI.Custodians.Domain.Models.EducationDetailsRecordV1>>;

namespace SUI.StubCustodians.Application.Queries
{
    [ExcludeFromCodeCoverage]
    public record GetHealthDataRecordQuery : GetRecordQueryBase, IRequest<ResultType>
    {
        public IReadOnlyCollection<ErrorInfo> ValidateCommand() => ValidateCommon();
    }

    [ExcludeFromCodeCoverage]
    public class GetHealthDataRecordQueryHandler
        : IRequestHandler<GetHealthDataRecordQuery, ResultType>
    {
        public Task<ResultType> Handle(
            GetHealthDataRecordQuery request,
            CancellationToken cancellationToken
        )
        {
            throw new NotImplementedException();
        }
    }
}
