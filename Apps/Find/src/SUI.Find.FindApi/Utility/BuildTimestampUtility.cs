using System.Globalization;
using System.Reflection;
using System.Text.RegularExpressions;

namespace SUI.Find.FindApi.Utility;

public static partial class BuildTimestampUtility
{
    public static DateTimeOffset BuildTimestamp { get; } = ExtractBuildTimestampFromAssembly();

    private static DateTimeOffset ExtractBuildTimestampFromAssembly()
    {
        // Note: `SourceRevisionId` must be set as expected in assembly's project file
        var match = ExtractBuildTimestampRegex()
            .Match(
                typeof(BuildTimestampUtility)
                    .Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()
                    ?.InformationalVersion
                    ?? ""
            );

        return DateTimeOffset.ParseExact(
            match.Groups["timestamp"].Value,
            "O",
            CultureInfo.InvariantCulture,
            DateTimeStyles.AdjustToUniversal
        );
    }

    [GeneratedRegex(@"\+timestamp_(?<timestamp>.+)")]
    private static partial Regex ExtractBuildTimestampRegex();
}
