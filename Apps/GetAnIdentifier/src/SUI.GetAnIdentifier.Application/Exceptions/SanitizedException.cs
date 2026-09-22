namespace SUI.GetAnIdentifier.Application.Exceptions;

public sealed class SanitizedException : Exception
{
    private readonly string? _stackTrace;

    public SanitizedException(string safeMessage, Exception original)
        : base($"[{original.GetType().Name}] {safeMessage}")
    {
        _stackTrace = original.StackTrace;
    }

    public override string? StackTrace => _stackTrace;
}

public static class ExceptionSanitizerExtensions
{
    public static SanitizedException Sanitize(
        this Exception ex,
        string safeMessage = "An error occurred (details redacted for privacy)."
    )
    {
        return new SanitizedException(safeMessage, ex);
    }
}
