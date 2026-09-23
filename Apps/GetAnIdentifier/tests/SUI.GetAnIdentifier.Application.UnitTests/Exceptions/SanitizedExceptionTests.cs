using SUI.GetAnIdentifier.Application.Exceptions;

namespace SUI.GetAnIdentifier.Application.UnitTests.Exceptions;

public class SanitizedExceptionTests
{
    // Helper method to generate an exception that actually has a populated StackTrace
    private static Exception GenerateExceptionWithStackTrace(string sensitiveMessage)
    {
        try
        {
            throw new InvalidOperationException(sensitiveMessage);
        }
        catch (Exception ex)
        {
            return ex;
        }
    }

    [Fact]
    public void Constructor_ReplacesMessage_AndIncludesOriginalExceptionType()
    {
        // Arrange
        const string sensitiveData = "PII_DATA_1234567890";
        var originalException = new ArgumentNullException("paramName", sensitiveData);
        const string safeMessage = "A safe validation error occurred.";

        // Act
        var sanitizedException = new SanitizedException(safeMessage, originalException);

        // Assert
        Assert.Equal(
            "[ArgumentNullException] A safe validation error occurred.",
            sanitizedException.Message
        );

        // Verify the sensitive data did not survive
        Assert.DoesNotContain(sensitiveData, sanitizedException.Message);
    }

    [Fact]
    public void Constructor_PreservesOriginalStackTrace()
    {
        // Arrange
        var originalException = GenerateExceptionWithStackTrace("Sensitive data");
        var safeMessage = "Safe message";

        // Act
        var sanitizedException = new SanitizedException(safeMessage, originalException);

        // Assert
        Assert.NotNull(sanitizedException.StackTrace);
        Assert.Equal(originalException.StackTrace, sanitizedException.StackTrace);
    }

    [Fact]
    public void SanitizeExtension_WithCustomMessage_CreatesCorrectSanitizedException()
    {
        // Arrange
        var originalException = new HttpRequestException(
            "Failed to reach http://sensitive-url.internal"
        );
        const string customSafeMessage = "Network call failed.";

        // Act
        var result = originalException.Sanitize(customSafeMessage);

        // Assert
        Assert.IsType<SanitizedException>(result);
        Assert.Equal("[HttpRequestException] Network call failed.", result.Message);
        Assert.DoesNotContain("sensitive-url", result.Message);
    }

    [Fact]
    public void SanitizeExtension_WithoutMessage_UsesDefaultPrivacyMessage()
    {
        // Arrange
        var originalException = new Exception("Super secret database connection string failed");

        // Act
        var result = originalException.Sanitize(); // Using the default parameter

        // Assert
        Assert.IsType<SanitizedException>(result);
        Assert.Equal(
            "[Exception] An error occurred (details redacted for privacy).",
            result.Message
        );
    }

    [Fact]
    public void Constructor_HandlesOriginalExceptionWithoutStackTrace()
    {
        // Arrange
        // An exception instantiated but not thrown will have a null StackTrace
        var unthrownException = new Exception("Just instantiated");

        // Act
        var sanitizedException = new SanitizedException("Safe", unthrownException);

        // Assert
        Assert.Null(sanitizedException.StackTrace);
    }
}
