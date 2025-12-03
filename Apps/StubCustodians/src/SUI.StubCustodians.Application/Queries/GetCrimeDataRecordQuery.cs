using System.Diagnostics.CodeAnalysis;
using MediatR;
using SUI.StubCustodians.Application.Common;
using ResultType = SUI.StubCustodians.Application.Common.HandlerResult<SUI.StubCustodians.Application.Models.RecordEnvelope<SUI.Custodians.Domain.Models.CrimeDataRecordV1>>;

namespace SUI.StubCustodians.Application.Queries
{
    [ExcludeFromCodeCoverage]
    public record GetCrimeDataRecordQuery : GetRecordQueryBase, IRequest<ResultType>
    {
        public IReadOnlyCollection<ErrorInfo> ValidateCommand() => ValidateCommon();
    }

    [ExcludeFromCodeCoverage]
    public class GetCrimeDataRecordQueryHandler
        : IRequestHandler<GetCrimeDataRecordQuery, ResultType>
    {
        public Task<ResultType> Handle(
            GetCrimeDataRecordQuery request,
            CancellationToken cancellationToken
        )
        {
            throw new NotImplementedException();
        }
    }
}
