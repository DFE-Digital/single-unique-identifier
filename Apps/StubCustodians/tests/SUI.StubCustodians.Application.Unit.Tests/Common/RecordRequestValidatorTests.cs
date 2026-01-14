using SUI.StubCustodians.Application.Common;

namespace SUI.StubCustodians.Application.Unit.Tests.Common
{
    public class RecordRequestValidatorTests
    {
        [Theory]
        [InlineData("1234567890", "MockCrimeDataProvider")]
        [InlineData("0987654321", "MockEducationProvider")]
        [InlineData("1111111111", "MockHealthcareProvider")]
        [InlineData("2222222222", "MockSocialCareProvider")]
        public void Validate_ValidInput_ReturnsNoErrors(string sui, string provider)
        {
            var errors = RecordRequestValidator.Validate(sui, provider);

            Assert.Empty(errors);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void Validate_SuiNullOrWhitespace_ReturnsError(string sui)
        {
            var provider = "MockCrimeDataProvider";

            var errors = RecordRequestValidator.Validate(sui, provider);

            Assert.Single(errors);
            Assert.Equal("sui", errors.First().Scope);
            Assert.Contains("cannot be null or whitespace", errors.First().Message);
        }

        [Theory]
        [InlineData("12345abcde")]
        [InlineData("12#4567890")]
        public void Validate_SuiContainsNonDigits_ReturnsError(string sui)
        {
            var provider = "MockCrimeDataProvider";

            var errors = RecordRequestValidator.Validate(sui, provider);

            Assert.Contains(
                errors,
                e => string.Equals(e.Scope, "sui") && e.Message.Contains("only contain digits")
            );
        }

        [Theory]
        [InlineData("123456789")] // 9 digits
        [InlineData("12345678901")] // 11 digits
        public void Validate_SuiIncorrectLength_ReturnsError(string sui)
        {
            var provider = "MockCrimeDataProvider";

            var errors = RecordRequestValidator.Validate(sui, provider);

            Assert.Contains(
                errors,
                e =>
                    string.Equals(e.Scope, "sui")
                    && e.Message.Contains("must have 10 digits", StringComparison.OrdinalIgnoreCase)
            );
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void Validate_ProviderSystemIdNullOrWhitespace_ReturnsError(string provider)
        {
            var sui = "1234567890";

            var errors = RecordRequestValidator.Validate(sui, provider);

            Assert.Single(errors);
            Assert.Equal("providerSystemId", errors.First().Scope);
            Assert.Contains("cannot be null or whitespace", errors.First().Message);
        }

        [Fact]
        public void Validate_ProviderSystemIdInvalid_ReturnsError()
        {
            var sui = "1234567890";
            var provider = "InvalidProvider";

            var errors = RecordRequestValidator.Validate(sui, provider);

            Assert.Single(errors);
            Assert.Equal("providerSystemId", errors.First().Scope);
            Assert.Contains("not supported", errors.First().Message);
        }

        [Fact]
        public void Validate_MultipleErrors_ReturnsAllErrors()
        {
            var sui = "abc"; // invalid sui
            var provider = "Wrong"; // invalid provider

            var errors = RecordRequestValidator.Validate(sui, provider);

            Assert.Equal(3, errors.Count);
            Assert.Contains(errors, e => string.Equals(e.Scope, "sui"));
            Assert.Contains(errors, e => string.Equals(e.Scope, "providerSystemId"));
        }
    }
}
