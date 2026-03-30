using System.Diagnostics;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using SUI.Find.Application.Extensions;

namespace SUI.Find.Application.UnitTests.Extensions;

public class LoggerActivityExtensionsTests
{
    [Fact]
    public void StartActivityWithTraceParent_Does_Setup_Activity_And_LogScope_AsExpected()
    {
        const string exampleActivityName = "TestActivityName";
        const string exampleTraceParent = "00-c8248340542c3d82ade91f0cf45473b2-a974f0b90d1628e6-01";
        var exampleLogMetadata = new Dictionary<string, object?> { { "test", true } };

        var mockLogger = Substitute.For<ILogger<LoggerActivityExtensionsTests>>();
        mockLogger.IsEnabled(LogLevel.Information).Returns(true);

        var mockLogScope = Substitute.For<IDisposable>();
        mockLogger.BeginScope(exampleLogMetadata).Returns(mockLogScope);

        // (pre-assert)
        Activity.Current.Should().BeNull();

        // ACT
        using (
            var activityScope = mockLogger.StartActivityWithTraceParent(
                exampleActivityName,
                exampleTraceParent,
                exampleLogMetadata
            )
        )
        {
            // ASSERT
            activityScope.Should().NotBeNull();

            Activity
                .Current.Should()
                .BeEquivalentTo(
                    new
                    {
                        TraceId = ActivityTraceId.CreateFromString(
                            "c8248340542c3d82ade91f0cf45473b2"
                        ),
                        ParentSpanId = ActivitySpanId.CreateFromString("a974f0b90d1628e6"),
                        ParentId = exampleTraceParent,
                    }
                );

            Activity
                .Current.SpanId.ToString()
                .Should()
                .NotBe("a974f0b90d1628e6")
                .And.NotBeNullOrWhiteSpace();

            mockLogger
                .Received()
                .Log(
                    LogLevel.Information,
                    Arg.Any<EventId>(),
                    Arg.Is<Arg.AnyType>(
                        (object x) =>
                            $"{x}"
                            == "Started activity TestActivityName with traceparent 00-c8248340542c3d82ade91f0cf45473b2-a974f0b90d1628e6-01"
                    ),
                    null,
                    Arg.Any<Func<Arg.AnyType, Exception?, string>>()
                );

            mockLogger.Received().BeginScope(exampleLogMetadata);
            mockLogScope.DidNotReceive().Dispose();
        }

        mockLogScope.Received().Dispose();

        // Once the activity is disposed, the current activity should no longer be the activity we created
        Activity.Current.Should().BeNull();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("  \t\t  ")]
    [InlineData("invalid")]
    public void StartActivityWithTraceParent_DoesNotThrow_WithInvalidOrEmptyTraceParent(
        string? inputTraceParent
    )
    {
        var mockLogger = Substitute.For<ILogger<LoggerActivityExtensionsTests>>();

        // ACT
        using var activityScope = mockLogger.StartActivityWithTraceParent(
            "TestActivityName",
            inputTraceParent,
            new Dictionary<string, object?> { { "test", true } }
        );

        // ASSERT
        activityScope.Should().NotBeNull();

        Activity.Current.Should().NotBeNull(); // An activity still gets created

        Activity.Current.ParentId.Should().BeNull(); // But it should have no parent
    }
}
