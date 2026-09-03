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

        ILogger? logger = null;

        try
        {
            using var host = hostFactory();
            var lifetime = host.Services.GetRequiredService<IHostApplicationLifetime>();
            logger = host
                .Services.GetRequiredService<ILoggerFactory>()
                .CreateLogger(nameof(NotificationServiceRunner));

            return await RunHostAsync(host, lifetime, logger);
        }
        catch (Exception exception) when (!IsFatal(exception))
        {
            LogFailure(logger, exception, "Notification Service host setup or disposal failed");
            return NotificationServiceExitCodes.UnhandledFailure;
        }
    }

    private static async Task<int> RunHostAsync(
        IHost host,
        IHostApplicationLifetime lifetime,
        ILogger logger
    )
    {
        var exitCode = NotificationServiceExitCodes.UnhandledFailure;

        try
        {
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
            when (lifetime.ApplicationStopping.IsCancellationRequested)
        {
            logger.LogInformation("Notification Service execution was cancelled");
            exitCode = NotificationServiceExitCodes.Cancelled;
        }
        catch (Exception exception) when (!IsFatal(exception))
        {
            LogFailure(logger, exception, "Notification Service execution failed");
            exitCode = NotificationServiceExitCodes.UnhandledFailure;
        }
        finally
        {
            try
            {
                await host.StopAsync(CancellationToken.None);
            }
            catch (OperationCanceledException exception)
            {
                logger.LogInformation(
                    exception,
                    "Notification Service host shutdown was cancelled"
                );
                exitCode = NotificationServiceExitCodes.Cancelled;
            }
            catch (Exception exception) when (!IsFatal(exception))
            {
                LogFailure(logger, exception, "Notification Service host shutdown failed");
                exitCode = NotificationServiceExitCodes.UnhandledFailure;
            }
        }

        return exitCode;
    }

    private static bool IsFatal(Exception exception) =>
        exception
            is OutOfMemoryException
                or StackOverflowException
                or AccessViolationException
                or AppDomainUnloadedException
                or BadImageFormatException
                or CannotUnloadAppDomainException
                or InvalidProgramException;

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
