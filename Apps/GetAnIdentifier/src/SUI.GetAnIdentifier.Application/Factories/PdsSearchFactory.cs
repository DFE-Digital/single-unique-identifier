using SUI.GetAnIdentifier.Application.Interfaces;

namespace SUI.GetAnIdentifier.Application.Factories;

public interface IPdsSearchFactory
{
    IPdsSearchStrategy GetVersion(int version);
}

public class PdsSearchFactory : IPdsSearchFactory
{
    private readonly Dictionary<int, IPdsSearchStrategy> _strategiesByVersion;

    public PdsSearchFactory(IEnumerable<IPdsSearchStrategy> strategies)
    {
        _strategiesByVersion = strategies.ToDictionary(s => s.Version);
    }

    public IPdsSearchStrategy GetVersion(int version)
    {
        return _strategiesByVersion.TryGetValue(version, out var strategy)
            ? strategy
            : throw new NotSupportedException($"PDS Search version {version} is not supported."); // Fail fast if version not found
    }
}
