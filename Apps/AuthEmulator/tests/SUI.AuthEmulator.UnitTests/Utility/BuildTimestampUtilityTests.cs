using Shouldly;
using SUI.AuthEmulator.Utility;
using Xunit.Abstractions;

namespace SUI.AuthEmulator.UnitTests.Utility;

public class BuildTimestampUtilityTests(ITestOutputHelper testOutputHelper)
{
    [Fact]
    public void BuildTimestamp_Should_BeAGeneratedDateTime()
    {
        testOutputHelper.WriteLine($"BuildTimestamp: {BuildTimestampUtility.BuildTimestamp:O}");

        BuildTimestampUtility.BuildTimestamp.ShouldBeGreaterThan(DateTimeOffset.MinValue);
        BuildTimestampUtility.BuildTimestamp.ShouldBeLessThanOrEqualTo(DateTimeOffset.UtcNow);
    }
}
