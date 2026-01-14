namespace SUI.StubCustodians.Application.Common
{
    public static class RecordRequestValidator
    {
        private static readonly HashSet<string> AllowedProviders =
        [
            "MockCrimeDataProvider",
            "MockEducationProvider",
            "MockHealthcareProvider",
            "MockSocialCareProvider",
        ];

        public static IReadOnlyCollection<ErrorInfo> Validate(
            string sui,
            string providerSystemId
        )
        {
            var errors = new List<ErrorInfo>();

            // SUI validation
            if (string.IsNullOrWhiteSpace(sui))
            {
                errors.Add(new ErrorInfo(nameof(sui), "Value cannot be null or whitespace."));
            }
            else
            {
                if (!sui.All(char.IsDigit))
                    errors.Add(new ErrorInfo(nameof(sui), "Value can only contain digits."));

                if (sui.Length != 10)
                    errors.Add(new ErrorInfo(nameof(sui), "Value must have 10 digits only."));
            }

            // ProviderSystemId validation
            if (string.IsNullOrWhiteSpace(providerSystemId))
            {
                errors.Add(
                    new ErrorInfo(
                        nameof(providerSystemId),
                        "Value cannot be null or whitespace."
                    )
                );
            }
            else if (
                !AllowedProviders.Contains(
                    providerSystemId.Trim(),
                    StringComparer.OrdinalIgnoreCase
                )
            )
            {
                errors.Add(
                    new ErrorInfo(
                        nameof(providerSystemId),
                        $"Value specified is not supported. Please use one of the following: {string.Join(", ", AllowedProviders)}"
                    )
                );
            }

            return errors;
        }
    }
}
