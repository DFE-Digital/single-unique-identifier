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
    public async Task RunAsync_ReturnsUnhandledFailure_WhenHostStartFails()
    {
        var orchestrator = Substitute.For<INotificationOrchestrator>();
        var host = BuildTestHost(
            orchestrator,
            startException: new InvalidOperationException("Host start failed")
        );

        var exitCode = await NotificationServiceRunner.RunAsync(() => host);

        Assert.Equal(NotificationServiceExitCodes.UnhandledFailure, exitCode);
        Assert.Equal(1, host.StartCallCount);
        Assert.Equal(1, host.StopCallCount);
        Assert.Equal(1, host.DisposeCallCount);
        await orchestrator.DidNotReceive().RunAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RunAsync_ReturnsUnhandledFailure_WhenHostStopFails()
    {
        var orchestrator = Substitute.For<INotificationOrchestrator>();
        orchestrator.RunAsync(Arg.Any<CancellationToken>()).Returns(Task.CompletedTask);
        var host = BuildTestHost(
            orchestrator,
            stopException: new InvalidOperationException("Host stop failed")
        );

        var exitCode = await NotificationServiceRunner.RunAsync(() => host);

        Assert.Equal(NotificationServiceExitCodes.UnhandledFailure, exitCode);
        Assert.Equal(1, host.StartCallCount);
        Assert.Equal(1, host.StopCallCount);
        Assert.Equal(1, host.DisposeCallCount);
        await orchestrator.Received(1).RunAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RunAsync_ReturnsUnhandledFailure_WhenHostDisposalFails()
    {
        var orchestrator = Substitute.For<INotificationOrchestrator>();
        orchestrator.RunAsync(Arg.Any<CancellationToken>()).Returns(Task.CompletedTask);
        var host = BuildTestHost(
            orchestrator,
            disposeException: new InvalidOperationException("Host disposal failed")
        );

        var exitCode = await NotificationServiceRunner.RunAsync(() => host);

        Assert.Equal(NotificationServiceExitCodes.UnhandledFailure, exitCode);
        Assert.Equal(1, host.StartCallCount);
        Assert.Equal(1, host.StopCallCount);
        Assert.Equal(1, host.DisposeCallCount);
        await orchestrator.Received(1).RunAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RunAsync_ReturnsCancelled_WhenHostStopIsCancelled()
    {
        var orchestrator = Substitute.For<INotificationOrchestrator>();
        orchestrator.RunAsync(Arg.Any<CancellationToken>()).Returns(Task.CompletedTask);
        var host = BuildTestHost(
            orchestrator,
            stopException: new OperationCanceledException("Host stop cancelled")
        );

        var exitCode = await NotificationServiceRunner.RunAsync(() => host);

        Assert.Equal(NotificationServiceExitCodes.Cancelled, exitCode);
        Assert.Equal(1, host.StartCallCount);
        Assert.Equal(1, host.StopCallCount);
        Assert.Equal(1, host.DisposeCallCount);
    }

    [Theory]
    [MemberData(nameof(FatalExceptionTypes))]
    public async Task RunAsync_PropagatesFatalExceptions(Type exceptionType)
    {
        var fatalException = (Exception)Activator.CreateInstance(exceptionType)!;
        var orchestrator = Substitute.For<INotificationOrchestrator>();
        orchestrator
            .RunAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromException(fatalException));
        var host = BuildTestHost(orchestrator);

        var thrownException = await Assert.ThrowsAsync(
            exceptionType,
            () => NotificationServiceRunner.RunAsync(() => host)
        );

        Assert.Same(fatalException, thrownException);
        Assert.Equal(1, host.StartCallCount);
        Assert.Equal(1, host.StopCallCount);
        Assert.Equal(1, host.DisposeCallCount);
    }

    [Fact]
    public async Task RunAsync_PropagatesFatalException_WhenHostStopFails()
    {
        var orchestrator = Substitute.For<INotificationOrchestrator>();
        orchestrator.RunAsync(Arg.Any<CancellationToken>()).Returns(Task.CompletedTask);
        var fatalException = new AccessViolationException("Fatal host stop failure");
        var host = BuildTestHost(orchestrator, stopException: fatalException);

        var thrownException = await Assert.ThrowsAsync<AccessViolationException>(() =>
            NotificationServiceRunner.RunAsync(() => host)
        );

        Assert.Same(fatalException, thrownException);
        Assert.Equal(1, host.StartCallCount);
        Assert.Equal(1, host.StopCallCount);
        Assert.Equal(1, host.DisposeCallCount);
    }

    [Fact]
    public async Task RunAsync_PropagatesFatalException_WhenHostDisposalFails()
    {
        var orchestrator = Substitute.For<INotificationOrchestrator>();
        orchestrator.RunAsync(Arg.Any<CancellationToken>()).Returns(Task.CompletedTask);
        var fatalException = new AccessViolationException("Fatal host disposal failure");
        var host = BuildTestHost(orchestrator, disposeException: fatalException);

        var thrownException = await Assert.ThrowsAsync<AccessViolationException>(() =>
            NotificationServiceRunner.RunAsync(() => host)
        );

        Assert.Same(fatalException, thrownException);
        Assert.Equal(1, host.StartCallCount);
        Assert.Equal(1, host.StopCallCount);
        Assert.Equal(1, host.DisposeCallCount);
    }

    public static TheoryData<Type> FatalExceptionTypes =>
        new()
        {
            typeof(OutOfMemoryException),
            typeof(StackOverflowException),
            typeof(AccessViolationException),
            typeof(AppDomainUnloadedException),
            typeof(BadImageFormatException),
            typeof(CannotUnloadAppDomainException),
            typeof(InvalidProgramException),
        };

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

    private static TestHost BuildTestHost(
        INotificationOrchestrator orchestrator,
        Exception? startException = null,
        Exception? stopException = null,
        Exception? disposeException = null
    )
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton(Substitute.For<IHostApplicationLifetime>());
        services.AddScoped(_ => orchestrator);

        var serviceProvider = services.BuildServiceProvider(
            new ServiceProviderOptions { ValidateScopes = true, ValidateOnBuild = true }
        );

        return new TestHost(serviceProvider, startException, stopException, disposeException);
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

    private sealed class TestHost(
        IServiceProvider services,
        Exception? startException,
        Exception? stopException,
        Exception? disposeException
    ) : IHost
    {
        private int _disposeCallCount;
        private int _startCallCount;
        private int _stopCallCount;

        public IServiceProvider Services { get; } = services;

        public int DisposeCallCount => _disposeCallCount;
        public int StartCallCount => _startCallCount;
        public int StopCallCount => _stopCallCount;

        public Task StartAsync(CancellationToken cancellationToken = default)
        {
            Interlocked.Increment(ref _startCallCount);
            return startException is null ? Task.CompletedTask : Task.FromException(startException);
        }

        public Task StopAsync(CancellationToken cancellationToken = default)
        {
            Interlocked.Increment(ref _stopCallCount);
            return stopException is null ? Task.CompletedTask : Task.FromException(stopException);
        }

        public void Dispose()
        {
            Interlocked.Increment(ref _disposeCallCount);
            (Services as IDisposable)?.Dispose();

            if (disposeException is not null)
            {
                throw disposeException;
            }
        }
    }
}
