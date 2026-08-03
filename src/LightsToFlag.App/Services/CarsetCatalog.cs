using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace LightsToFlag.App.Services;

/// <summary>A carset discovered on disk.</summary>
public sealed record CarsetInfo(string Name, string Path);

/// <summary>Finds installed carsets (folders containing a <c>Rules.txt</c>).</summary>
public sealed class CarsetCatalog
{
    private readonly string _root;

    public CarsetCatalog()
    {
        _root = Path.Combine(AppContext.BaseDirectory, "carsets");
    }

    public IReadOnlyList<CarsetInfo> List()
    {
        if (!Directory.Exists(_root))
        {
            return System.Array.Empty<CarsetInfo>();
        }

        return Directory.EnumerateDirectories(_root)
            .Where(dir => File.Exists(Path.Combine(dir, "Rules.txt")))
            .Select(dir => new CarsetInfo(new DirectoryInfo(dir).Name, dir))
            .OrderBy(c => c.Name, System.StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    public CarsetInfo? FindByName(string name) =>
        List().FirstOrDefault(c => string.Equals(c.Name, name, System.StringComparison.OrdinalIgnoreCase));
}
