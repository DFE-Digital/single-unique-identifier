using System.Reflection;

namespace SUI.Find.FindApi.Utility;

public static class BuildTimestampUtility
{
    public static string BuildTimestamp { get; } =
        GetBuildTimestamp(typeof(BuildTimestampUtility).Assembly);

    /// <remarks>
    /// Note that the `SourceRevisionId` must be set accordingly in the assembly's project file.
    /// </remarks>
    // https://www.meziantou.net/getting-the-date-of-build-of-a-dotnet-assembly-at-runtime.htm#method-2-assemblyinf
    private static string GetBuildTimestamp(Assembly assembly)
    {
        const string buildVersionMetadataPrefix = "+build_timestamp_";

        var versionAttribute = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>();
        if (versionAttribute?.InformationalVersion != null)
        {
            var value = versionAttribute.InformationalVersion;
            var index = value.IndexOf(buildVersionMetadataPrefix, StringComparison.Ordinal);
            if (index > 0)
            {
                return value[(index + buildVersionMetadataPrefix.Length)..];
            }
        }

        return "unknown_build_timestamp";
    }
}
