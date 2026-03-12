using Shouldly;
using SUI.Find.Infrastructure.Configuration;

namespace SUI.Find.Infrastructure.UnitTests.Configuration;

public class JobClaimConfigTests
{
    [Fact]
    public void JobClaimConfig_Test()
    {
        var sut = new JobClaimConfig
        {
            AvailableJobWindowStartOffsetHours = 1200,
            LeaseDurationMinutes = 1400,
            MaxClaimAttemptsPerJob = 100,
            MaxReScanAttempts = 50,
        };

        // ASSERT
        sut.AvailableJobWindowStartOffsetHours.ShouldBe(1200);
        sut.LeaseDurationMinutes.ShouldBe(1400);
        sut.MaxClaimAttemptsPerJob.ShouldBe(100);
        sut.MaxReScanAttempts.ShouldBe(50);
    }

    [Fact]
    public void AvailableJobWindowStartOffsetHours_Setter_UsesAbsoluteValue()
    {
        var sut = new JobClaimConfig { AvailableJobWindowStartOffsetHours = -500 };

        // ASSERT
        sut.AvailableJobWindowStartOffsetHours.ShouldBe(500);
    }

    [Fact]
    public void LeaseDurationMinutes_Setter_UsesAbsoluteValue()
    {
        var sut = new JobClaimConfig { LeaseDurationMinutes = -70 };

        // ASSERT
        sut.LeaseDurationMinutes.ShouldBe(70);
    }
}
