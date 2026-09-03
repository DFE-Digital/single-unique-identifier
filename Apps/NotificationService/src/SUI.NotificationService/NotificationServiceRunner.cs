using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SUI.NotificationService.Application;

namespace SUI.NotificationService;

internal static class NotificationServiceRunner
{
    public static async Task<int> RunAsync(Func<IHost> hostFactory)
    {
        ArgumentNullException.ThrowIfNull(hostFactory);

        IHost? host = null;
        IHostApplicationLifetime? lifetime = null;
        ILogger? logger = null;
        var exitCode = NotificationServiceExitCodes.UnhandledFailure;

        try
        {
            host = hostFactory();
            lifetime = host.Services.GetRequiredService<IHostApplicationLifetime>();
            logger = host
                .Services.GetRequiredService<ILoggerFactory>()
                .CreateLogger(nameof(NotificationServiceRunner));

            await host.StartAsync();

            await using var scope = host.Services.CreateAsyncScope();
            var orchestrator =
                scope.ServiceProvider.GetRequiredService<INotificationOrchestrator>();

            await orchestrator.RunAsync(lifetime.ApplicationStopping);

            exitCode = lifetime.ApplicationStopping.IsCancellationRequested
                ? NotificationServiceExitCodes.Cancelled
                : NotificationServiceExitCodes.Success;
        }
        catch (OperationCanceledException)
            when (lifetime?.ApplicationStopping.IsCancellationRequested is true)
        {
            logger?.LogInformation("Notification Service execution was cancelled");
            exitCode = NotificationServiceExitCodes.Cancelled;
        }
        catch (Exception exception)
        {
            LogFailure(logger, exception, "Notification Service execution failed");
            exitCode = NotificationServiceExitCodes.UnhandledFailure;
        }
        finally
        {
            if (host is not null)
            {
                try
                {
                    await host.StopAsync(CancellationToken.None);
                }
                catch (Exception exception)
                {
                    LogFailure(logger, exception, "Notification Service host shutdown failed");
                    exitCode = NotificationServiceExitCodes.UnhandledFailure;
                }
                finally
                {
                    try
                    {
                        host.Dispose();
                    }
                    catch (Exception exception)
                    {
                        LogFailure(logger, exception, "Notification Service host disposal failed");
                        exitCode = NotificationServiceExitCodes.UnhandledFailure;
                    }
                }
            }
        }

        return exitCode;
    }

    private static void LogFailure(ILogger? logger, Exception exception, string message)
    {
        if (logger is null)
        {
            Console.Error.WriteLine($"{message}: {exception}");
            return;
        }

        logger.LogCritical(exception, message);
    }
}
