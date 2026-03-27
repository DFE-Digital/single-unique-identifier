using System.Text.Json;
using SUI.StubCustodians.Application.Interfaces;
using SUI.StubCustodians.Application.Models;

namespace SUI.StubCustodians.Application.Services;

public class OrgDirectoryProvider : IOrgDirectoryProvider
{
    private readonly Lazy<IReadOnlyList<Organisation>> _orgs;

    public OrgDirectoryProvider()
    {
        _orgs = new Lazy<IReadOnlyList<Organisation>>(Load);
    }

    public IReadOnlyList<Organisation> GetOrganisations() => _orgs.Value;

    private static IReadOnlyList<Organisation> Load()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "Data", "org-directory.json");

        if (!File.Exists(path))
        {
            throw new InvalidOperationException($"org-directory.json not found: {path}");
        }

        var json = File.ReadAllText(path);

        var data =
            JsonSerializer.Deserialize<OrgDirectory>(json, JsonSerializerOptions.Web)
            ?? throw new InvalidOperationException("Invalid org-directory.json");

        return data.Organisations;
    }
}
