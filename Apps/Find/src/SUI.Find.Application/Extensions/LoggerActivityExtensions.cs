using System.Diagnostics;
using Microsoft.Extensions.Logging;

namespace SUI.Find.Application.Extensions;

public static class LoggerActivityExtensions
{
    public static IDisposable StartActivityWithTraceParent<TLogger>(
        this ILogger<TLogger> logger,
        string activityName,
        string? traceParent,
        Dictionary<string, object?> logMetadata
    )
    {
        var activity = Tracing.StartActivity(activityName, traceParent);
        var logScope = logger.BeginScope(logMetadata);

        if (logger.IsEnabled(LogLevel.Information))
            logger.LogInformation(
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

    private static class Tracing
    {
        static Tracing()
        {
            var activityListener = new ActivityListener
            {
                ShouldListenTo = s => s == ActivitySource,
                SampleUsingParentId = (ref _) => ActivitySamplingResult.AllData,
                Sample = (ref _) => ActivitySamplingResult.AllData,
            };
            ActivitySource.AddActivityListener(activityListener);
        }

        private static readonly ActivitySource ActivitySource = new("SUI.Find");

        public static Activity? StartActivity(string activityName, string? traceParent)
        {
            ActivityContext.TryParse(traceParent, null, out var parentContext);
            return ActivitySource.StartActivity(activityName, ActivityKind.Internal, parentContext);
        }
    }
}
