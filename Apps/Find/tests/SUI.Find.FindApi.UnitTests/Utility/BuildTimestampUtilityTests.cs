using System.Globalization;
using Shouldly;
using SUI.Find.FindApi.Utility;
using Xunit.Abstractions;

namespace SUI.Find.FindApi.UnitTests.Utility;

public class BuildTimestampUtilityTests(ITestOutputHelper testOutputHelper)
{
    [Fact]
    public void BuildTimestamp_Should_BeAGeneratedDateTime()
    {
        BuildTimestampUtility.BuildTimestamp.ShouldNotContain("unknown");

        DateTime
            .TryParseExact(
                BuildTimestampUtility.BuildTimestamp,
                "O",
                CultureInfo.InvariantCulture,
                DateTimeStyles.AdjustToUniversal,
                out var buildTimestampParsed
            )
            .ShouldBe(true);

        testOutputHelper.WriteLine(
            $"BuildTimestamp: {BuildTimestampUtility.BuildTimestamp} -> {buildTimestampParsed}"
        );
    }
}
