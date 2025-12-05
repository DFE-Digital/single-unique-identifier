using System.Diagnostics.CodeAnalysis;
using SUI.StubCustodians.Application.Common;

namespace SUI.StubCustodians.Application.Queries
{
    [ExcludeFromCodeCoverage]
    public abstract record GetRecordQueryBase
    {
        public required string Sui { get; init; }

        public required string ProviderSystemId { get; init; }

        private static readonly HashSet<string> AllowedProviders =
        [
            "MockCrimeDataProvider",
            "MockEducationProvider",
            "MockHealthcareProvider",
            "MockSocialCareProvider",
        ];

        protected IReadOnlyCollection<ErrorInfo> ValidateCommon()
        {
            var errors = new List<ErrorInfo>();

            // SUI validation
            if (string.IsNullOrWhiteSpace(Sui))
            {
                errors.Add(new ErrorInfo(nameof(Sui), "Value cannot be null or whitespace."));
            }
            else
            {
                if (!Sui.All(char.IsDigit))
                    errors.Add(new ErrorInfo(nameof(Sui), "Value can only contain digits."));

                if (Sui.Length != 10)
                    errors.Add(new ErrorInfo(nameof(Sui), "Value must have 10 digits only."));
            }

            // ProviderSystemId validation
            if (string.IsNullOrWhiteSpace(ProviderSystemId))
            {
                errors.Add(
                    new ErrorInfo(nameof(ProviderSystemId), "Value cannot be null or whitespace.")
                );
            }
            else if (
                !AllowedProviders.Contains(
                    ProviderSystemId.Trim(),
                    StringComparer.OrdinalIgnoreCase
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
}
