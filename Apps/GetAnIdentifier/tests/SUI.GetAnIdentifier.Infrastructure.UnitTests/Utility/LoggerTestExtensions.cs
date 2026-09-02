using Microsoft.Extensions.Logging;
using NSubstitute;

namespace SUI.GetAnIdentifier.Infrastructure.UnitTests.Utility;

public static class LoggerTestExtensions
{
    /// <summary>
    /// Verifies that a log call with a specific level and message template was made.
    /// </summary>
    /// <typeparam name="T">The type of the logger's category.</typeparam>
    /// <param name="logger">The ILogger substitute.</param>
    /// <param name="expectedLogLevel">The expected log level (e.g., LogLevel.Error).</param>
    /// <param name="expectedMessageTemplate">The exact message template string to verify.</param>
    public static void VerifyLog<T>(
        this ILogger<T> logger,
        LogLevel expectedLogLevel,
        string expectedMessageTemplate
    )
    {
        // Find all calls to the core Log method with the specified LogLevel.
        var logCalls = logger
            .ReceivedCalls()
            .Where(call =>
                call.GetMethodInfo().Name == "Log"
                && (LogLevel)call.GetArguments()[0]! == expectedLogLevel
            )
            .ToList();

        Assert.NotEmpty(logCalls);

        // Check if any of the found calls match the message template.
        var matchFound = logCalls.Any(call =>
        {
            var state = call.GetArguments()[2];
            return state is IEnumerable<KeyValuePair<string, object>> kvp
                && kvp.Any(p =>
                    p.Key == "{OriginalFormat}" && p.Value.ToString() == expectedMessageTemplate
                );
        });

        Assert.True(
            matchFound,
            $"a log call with the message template '{expectedMessageTemplate}' was expected but not found."
        );
    }
}
