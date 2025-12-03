using System.Diagnostics.CodeAnalysis;
using MediatR;
using SUI.StubCustodians.Application.Common;
using ResultType = SUI.StubCustodians.Application.Common.HandlerResult<SUI.StubCustodians.Application.Models.RecordEnvelope<SUI.Custodians.Domain.Models.ChildSocialCareDetailsRecordV1>>;

namespace SUI.StubCustodians.Application.Queries
{
    [ExcludeFromCodeCoverage]
    public record GetChildSocialCareRecordQuery : IRequest<ResultType>
    {
        public required string Sui { get; init; }

        public required string ProviderSystemId { get; init; }

        /// <summary>
        /// Validation is temporarily based on the NHS number being the SUI.
        /// Ideally NHS number should be validated against the NHS number system.
        /// </summary>
        public IReadOnlyCollection<ErrorInfo> ValidateCommand()
        {
            var errors = new List<ErrorInfo>();

            if (string.IsNullOrWhiteSpace(Sui))
            {
                errors.Add(new ErrorInfo(nameof(Sui), "Value cannot be null or whitespace."));
            }

            if (!Sui.All(char.IsDigit))
            {
                errors.Add(new ErrorInfo(nameof(Sui), "Value can only contain digits."));
            }

            if (Sui.Length != 10)
            {
                errors.Add(new ErrorInfo(nameof(Sui), "Value must have 10 digits only."));
            }

            if (string.IsNullOrWhiteSpace(ProviderSystemId))
            {
                errors.Add(
                    new ErrorInfo(nameof(ProviderSystemId), "Value cannot be null or whitespace.")
                );
            }

            if (
                !string.Equals(
                    ProviderSystemId.Trim(),
                    "MockCrimeDataProvider",
                    StringComparison.OrdinalIgnoreCase
                )
                || !string.Equals(
                    ProviderSystemId.Trim(),
                    "MockEducationProvider",
                    StringComparison.OrdinalIgnoreCase
                )
                || !string.Equals(
                    ProviderSystemId.Trim(),
                    "MockHealthcareProvider",
                    StringComparison.OrdinalIgnoreCase
                )
                || !string.Equals(
                    ProviderSystemId.Trim(),
                    "MockSocialCareProvider",
                    StringComparison.OrdinalIgnoreCase
                )
            )
            {
                errors.Add(
                    new ErrorInfo(nameof(ProviderSystemId), "Value specified is not supported.")
                );
            }

            return errors;
        }
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
