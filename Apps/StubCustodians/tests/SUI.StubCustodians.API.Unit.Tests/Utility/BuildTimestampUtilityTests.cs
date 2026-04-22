using Shouldly;
using SUI.StubCustodians.API.Utility;
using Xunit.Abstractions;

namespace SUI.StubCustodians.API.Unit.Tests.Utility;

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
