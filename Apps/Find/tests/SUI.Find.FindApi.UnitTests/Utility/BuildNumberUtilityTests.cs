using System.Globalization;
using Shouldly;
using SUI.Find.FindApi.Utility;
using Xunit.Abstractions;

namespace SUI.Find.FindApi.UnitTests.Utility;

public class BuildNumberUtilityTests(ITestOutputHelper testOutputHelper)
{
    [Fact]
    public void BuildNumber_Should_BeAGeneratedDateTime()
    {
        BuildNumberUtility.BuildNumber.ShouldNotBeNull();

        DateTime
            .TryParseExact(
                BuildNumberUtility.BuildNumber,
                "O",
                CultureInfo.InvariantCulture,
                DateTimeStyles.AdjustToUniversal,
                out var buildNumberParsed
            )
            .ShouldBe(true);

        testOutputHelper.WriteLine(
            $"BuildNumber: {BuildNumberUtility.BuildNumber} -> {buildNumberParsed}"
        );
    }
}
