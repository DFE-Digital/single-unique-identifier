using System.Diagnostics.CodeAnalysis;
using MediatR;
using SUI.Custodians.Domain.Models;
using SUI.StubCustodians.Application.Common;
using SUI.StubCustodians.Application.Interfaces;
using ResultType = SUI.StubCustodians.Application.Common.HandlerResult<SUI.StubCustodians.Application.Models.RecordEnvelope<SUI.Custodians.Domain.Models.EducationDetailsRecordV1>>;

namespace SUI.StubCustodians.Application.Queries
{
    [ExcludeFromCodeCoverage]
    public record GetEducationDetailsRecordQuery : GetRecordQueryBase, IRequest<ResultType>
    {
        public IReadOnlyCollection<ErrorInfo> ValidateCommand() => ValidateCommon();
    }

    public class GetEducationDetailsRecordQueryHandler
        : IRequestHandler<GetEducationDetailsRecordQuery, ResultType>
    {
        private readonly IRecordProvider<EducationDetailsRecordV1> _recordProvider;

        public GetEducationDetailsRecordQueryHandler(
            IRecordProvider<EducationDetailsRecordV1> recordProvider
        )
        {
            _recordProvider = recordProvider;
        }

        public Task<ResultType> Handle(
            GetEducationDetailsRecordQuery request,
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
                        $"Records of type {typeof(EducationDetailsRecordV1)} for SUI:'{request.Sui}' not found"
                    )
                );
            }

            return Task.FromResult(ResultType.Success(result));
        }
    }
}
