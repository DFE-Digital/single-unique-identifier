using Microsoft.Extensions.Options;
using NSubstitute;
using Shouldly;
using SUI.Find.Infrastructure.Configuration;
using SUI.Find.Infrastructure.Services;

namespace SUI.Find.Infrastructure.UnitTests.Services;

public class JobWindowStartServiceTests
{
    [Fact]
    public void GetWindowStart_Returns_ExpectedValue()
    {
        var mockOptions = Substitute.For<IOptionsMonitor<JobClaimConfig>>();
        mockOptions.CurrentValue.Returns(
            new JobClaimConfig { AvailableJobWindowStartOffsetHours = 12.5 }
        );

        var mockTimeProvider = Substitute.For<TimeProvider>();
        mockTimeProvider
            .GetUtcNow()
            .Returns(new DateTimeOffset(2026, 3, 12, 6, 30, 45, TimeSpan.Zero));

        var sut = new JobWindowStartService(mockOptions, mockTimeProvider);

        // ACT
        var result = sut.GetWindowStart();

        // ASSERT
        result.ShouldBe(new DateTimeOffset(2026, 3, 11, 18, 0, 45, TimeSpan.Zero));
    }
}
