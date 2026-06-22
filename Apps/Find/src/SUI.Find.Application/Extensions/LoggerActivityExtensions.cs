using System.Diagnostics;
using Microsoft.Extensions.Logging;

namespace SUI.Find.Application.Extensions;

public static class LoggerActivityExtensions
{
    public const string SourceName = "SUI.Find";

    private static readonly ActivitySource ActivitySource = new(SourceName);

    private static Activity? StartActivity(
        string activityName,
        string? traceParent,
        ActivityKind activityKind
    )
    {
        ActivityContext.TryParse(traceParent, null, out var parentContext);
        return ActivitySource.StartActivity(activityName, activityKind, parentContext);
    }

    /// <summary>
    /// Starts an activity scope within which all logging and tracing will be correlated with the original Trace ID in the specified TraceParent.
    /// The purpose of this is to enable correlation of logging and tracing across disconnected processes.
    /// </summary>
    /// <param name="logger">The logger to use to create the logging scope.</param>
    /// <param name="activityName">The name of the activity</param>
    /// <param name="traceParent">The W3C 'TraceParent', containing the original Trace ID and parent span ID.</param>
    /// <param name="activityKind">The kind of the activity. For entry points to background tasks, like consuming from a queue, use ActivityKind.Consumer; otherwise, use ActivityKind.Internal.</param>
    /// <param name="logMetadata">Additional metadata to associate with the activity scope and all log statements within it.</param>
    /// <typeparam name="TLogger"></typeparam>
    /// <returns>A disposable object which should be disposed of when the scope of the activity has ended.</returns>
    public static IDisposable StartActivityWithTraceParent<TLogger>(
        this ILogger<TLogger> logger,
        string activityName,
        string? traceParent,
        ActivityKind activityKind,
        Dictionary<string, object?> logMetadata
    )
    {
        var activity = StartActivity(activityName, traceParent, activityKind);
        var logScope = logger.BeginScope(logMetadata);

        if (logger.IsEnabled(LogLevel.Debug))
            logger.LogDebug(
                "Started activity {ActivityName} with traceparent {TraceParent}",
                activityName,
                traceParent
            );

        return new ActivityScope(activity, logScope);
    }

    private sealed class ActivityScope(Activity? activity, IDisposable? logScope) : IDisposable
    {
        public void Dispose()
        {
            activity?.Dispose();
            logScope?.Dispose();
        }
    }
}
