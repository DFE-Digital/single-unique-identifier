using System.Reflection;

namespace SUI.Find.FindApi.Utility;

public static class BuildNumberUtility
{
    public static string? BuildNumber { get; } =
        typeof(BuildNumberUtility)
            .Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()
            ?.InformationalVersion.Split('+') // Note: `SourceRevisionId` must be set in assembly's project file
            .LastOrDefault();
}
