using SUI.GetAnIdentifier.API.Utility;
using Xunit.Abstractions;

namespace SUI.GetAnIdentifier.API.UnitTests.Utility;

public class BuildTimestampUtilityTests(ITestOutputHelper testOutputHelper)
{
    [Fact]
    public void BuildTimestamp_Should_BeAGeneratedDateTime()
    {
        testOutputHelper.WriteLine($"BuildTimestamp: {BuildTimestampUtility.BuildTimestamp:O}");

        Assert.True(BuildTimestampUtility.BuildTimestamp > DateTimeOffset.MinValue);
        Assert.True(BuildTimestampUtility.BuildTimestamp <= DateTimeOffset.UtcNow);
    }
}
