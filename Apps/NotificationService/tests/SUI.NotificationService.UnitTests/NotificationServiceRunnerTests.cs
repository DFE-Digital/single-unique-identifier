using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NSubstitute;
using SUI.NotificationService.Application;

namespace SUI.NotificationService.UnitTests;

public sealed class NotificationServiceRunnerTests
{
    [Fact]
    public async Task RunAsync_ReturnsSuccess_WhenOrchestrationCompletes()
    {
        var orchestrator = Substitute.For<INotificationOrchestrator>();
        orchestrator.RunAsync(Arg.Any<CancellationToken>()).Returns(Task.CompletedTask);
        var hostLifetime = new TestHostLifetime();
        var host = BuildHost(orchestrator, hostLifetime);

        var exitCode = await NotificationServiceRunner.RunAsync(() => host);

        Assert.Equal(NotificationServiceExitCodes.Success, exitCode);
        await orchestrator.Received(1).RunAsync(Arg.Any<CancellationToken>());
        Assert.Equal(1, hostLifetime.StopCallCount);
    }

    [Fact]
    public async Task RunAsync_ReturnsUnhandledFailure_WhenOrchestrationThrows()
    {
        var orchestrator = Substitute.For<INotificationOrchestrator>();
        orchestrator
            .RunAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromException(new InvalidOperationException("Orchestration failed")));
        var hostLifetime = new TestHostLifetime();
        var host = BuildHost(orchestrator, hostLifetime);

        var exitCode = await NotificationServiceRunner.RunAsync(() => host);

        Assert.Equal(NotificationServiceExitCodes.UnhandledFailure, exitCode);
        Assert.Equal(1, hostLifetime.StopCallCount);
    }

    [Fact]
    public async Task RunAsync_ReturnsCancelled_WhenExecutionIsCancelled()
    {
        var executionStarted = CreateCompletionSource();
        var orchestrator = Substitute.For<INotificationOrchestrator>();
        orchestrator
            .RunAsync(Arg.Any<CancellationToken>())
            .Returns(callInfo =>
                WaitUntilCancelledAsync(callInfo.Arg<CancellationToken>(), executionStarted)
            );
        var hostLifetime = new TestHostLifetime();
        var host = BuildHost(orchestrator, hostLifetime);

        var runTask = NotificationServiceRunner.RunAsync(() => host);
        await executionStarted.Task.WaitAsync(TimeSpan.FromSeconds(5));
        host.Services.GetRequiredService<IHostApplicationLifetime>().StopApplication();

        var exitCode = await runTask.WaitAsync(TimeSpan.FromSeconds(5));

        Assert.Equal(NotificationServiceExitCodes.Cancelled, exitCode);
        Assert.Equal(1, hostLifetime.StopCallCount);
    }

    [Fact]
    public async Task RunAsync_ReturnsCancelled_WhenOrchestratorHandlesCancellation()
    {
        var executionStarted = CreateCompletionSource();
        var orchestrator = Substitute.For<INotificationOrchestrator>();
        orchestrator
            .RunAsync(Arg.Any<CancellationToken>())
            .Returns(callInfo =>
                ObserveCancellationAsync(callInfo.Arg<CancellationToken>(), executionStarted)
            );
        var hostLifetime = new TestHostLifetime();
        var host = BuildHost(orchestrator, hostLifetime);

        var runTask = NotificationServiceRunner.RunAsync(() => host);
        await executionStarted.Task.WaitAsync(TimeSpan.FromSeconds(5));
        host.Services.GetRequiredService<IHostApplicationLifetime>().StopApplication();

        var exitCode = await runTask.WaitAsync(TimeSpan.FromSeconds(5));

        Assert.Equal(NotificationServiceExitCodes.Cancelled, exitCode);
        Assert.Equal(1, hostLifetime.StopCallCount);
    }

    [Fact]
    public async Task RunAsync_ReturnsUnhandledFailure_ForUnrelatedCancellation()
    {
        var orchestrator = Substitute.For<INotificationOrchestrator>();
        orchestrator
            .RunAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromException(new OperationCanceledException("Unrelated cancellation")));
        var hostLifetime = new TestHostLifetime();
        var host = BuildHost(orchestrator, hostLifetime);

        var exitCode = await NotificationServiceRunner.RunAsync(() => host);

        Assert.Equal(NotificationServiceExitCodes.UnhandledFailure, exitCode);
        Assert.Equal(1, hostLifetime.StopCallCount);
    }

    [Fact]
    public async Task RunAsync_ReturnsUnhandledFailure_WhenHostCreationFails()
    {
        var exitCode = await NotificationServiceRunner.RunAsync(() =>
            throw new InvalidOperationException("Host creation failed")
        );

        Assert.Equal(NotificationServiceExitCodes.UnhandledFailure, exitCode);
    }

    [Fact]
    public void Build_RegistersConsoleLifetime()
    {
        using var host = NotificationServiceHost.Build([]);

        var hostLifetime = host.Services.GetRequiredService<IHostLifetime>();

        Assert.Contains("ConsoleLifetime", hostLifetime.GetType().Name);
    }

    private static IHost BuildHost(
        INotificationOrchestrator orchestrator,
        TestHostLifetime hostLifetime
    )
    {
        var builder = Host.CreateApplicationBuilder(
            new HostApplicationBuilderSettings { EnvironmentName = Environments.Development }
        );
        builder.Logging.ClearProviders();
        builder.Services.AddNotificationServiceApplication();
        builder.Services.RemoveAll<IHostLifetime>();
        builder.Services.AddSingleton<IHostLifetime>(hostLifetime);
        builder.Services.RemoveAll<INotificationOrchestrator>();
        builder.Services.AddScoped(_ => orchestrator);

        return builder.Build();
    }

    private static TaskCompletionSource CreateCompletionSource() =>
        new(TaskCreationOptions.RunContinuationsAsynchronously);

    private static async Task WaitUntilCancelledAsync(
        CancellationToken cancellationToken,
        TaskCompletionSource executionStarted
    )
    {
        executionStarted.SetResult();
        await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
    }

    private static async Task ObserveCancellationAsync(
        CancellationToken cancellationToken,
        TaskCompletionSource executionStarted
    )
    {
        var cancellationObserved = CreateCompletionSource();
        using var registration = cancellationToken.Register(cancellationObserved.SetResult);

        executionStarted.SetResult();
        await cancellationObserved.Task;
    }

    private sealed class TestHostLifetime : IHostLifetime
    {
        private int _stopCallCount;

        public int StopCallCount => _stopCallCount;

        public Task WaitForStartAsync(CancellationToken cancellationToken) => Task.CompletedTask;

        public Task StopAsync(CancellationToken cancellationToken)
        {
            Interlocked.Increment(ref _stopCallCount);
            return Task.CompletedTask;
        }
    }
}
