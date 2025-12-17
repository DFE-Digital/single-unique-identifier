using System.Collections;
using System.Collections.Frozen;
using Microsoft.Extensions.Logging;
using SUI.Find.Application.Models;
using SUI.Find.Domain.Models;
using SUI.Find.Infrastructure.Utility;

namespace SUI.Find.Infrastructure.Services;
public class PolicyCompiler()
{
    public CompiledPolicyArtifact Compile(IEnumerable<ProviderDefinition> providers)
    {
        var allowedKeys = new HashSet<string>();
        var now = DateTimeOffset.UtcNow;

        var providerList = providers.ToList();

        foreach (var sourceProvider in providerList)
        {
            // 
            var activeDsaAgreements = sourceProvider.DsaPolicy.Defaults
                .Concat(sourceProvider.DsaPolicy.Exceptions)
                .Where(dsa => IsActive(dsa, now));

            foreach (var rule in activeDsaAgreements)
            {
                // we are creating an allow list at the moment 
                if (!string.Equals(rule.Effect, "allow", StringComparison.OrdinalIgnoreCase)) continue;

                foreach (var mode in rule.Modes)
                    foreach (var type in rule.DataTypes)
                        foreach (var purpose in rule.Purposes)
                        {
                            // Resolve who this rule applies to
                            var destinations = ResolveDestinations(rule, providerList);

                            foreach (var destOrgId in destinations)
                            {
                                var key = PolicyKeyFactory.CreateKey(
                                    sourceProvider.OrgId,
                                    destOrgId,
                                    mode,
                                    type,
                                    purpose
                                );

                                allowedKeys.Add(key);
                            }
                        }
            }
        }

        return new CompiledPolicyArtifact
        {
            CompiledAtUtc = DateTime.UtcNow,
            PolicyVersionId = Guid.NewGuid().ToString(),
            AllowedRequests = allowedKeys.ToFrozenSet()
        };
    }

    private static bool IsActive(DsaRuleDefinition rule, DateTimeOffset now)
    {
        return (rule.ValidFrom == null || rule.ValidFrom <= now) &&
               (rule.ValidUntil == null || rule.ValidUntil >= now);
    }

    private IEnumerable<string> ResolveDestinations(DsaRuleDefinition rule, List<ProviderDefinition> allProviders)
    {
        var dests = new HashSet<string>(rule.DestOrgIds);

        if (rule.DestOrgTypes.Count > 0)
        {
            var matched = allProviders
                .Where(provider => rule.DestOrgTypes.Contains(provider.OrgType, StringComparer.OrdinalIgnoreCase))
                .Select(provider => provider.OrgId);
            // Union With prevents duplicated destd values - cool
            dests.UnionWith(matched);
        }

        return dests;
    }
}