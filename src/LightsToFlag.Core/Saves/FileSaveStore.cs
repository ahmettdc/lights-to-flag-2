using LightsToFlag.Core.Career;

namespace LightsToFlag.Core.Saves;

/// <summary>
/// File-backed <see cref="ISaveStore"/> writing one <c>.json</c> per slot under a
/// root directory (the WPF app points this at <c>%AppData%/LightsToFlag/Saves</c>;
/// tests point it at a temp folder).
/// </summary>
public sealed class FileSaveStore : ISaveStore
{
    private const string Extension = ".json";
    private readonly string _root;

    public FileSaveStore(string rootDirectory)
    {
        _root = rootDirectory;
        Directory.CreateDirectory(_root);
    }

    public void Save(string slot, CareerState state)
    {
        var json = CareerSaveSerializer.Serialize(state);
        File.WriteAllText(PathFor(slot), json);
    }

    public CareerState Load(string slot)
    {
        var path = PathFor(slot);
        if (!File.Exists(path))
        {
            throw new FileNotFoundException($"No save in slot '{slot}'.", path);
        }

        return CareerSaveSerializer.Deserialize(File.ReadAllText(path));
    }

    public bool Exists(string slot) => File.Exists(PathFor(slot));

    public IReadOnlyList<string> ListSlots() =>
        Directory.Exists(_root)
            ? Directory.EnumerateFiles(_root, "*" + Extension)
                .Select(Path.GetFileNameWithoutExtension)
                .Where(name => !string.IsNullOrEmpty(name))
                .Select(name => name!)
                .OrderBy(name => name, StringComparer.OrdinalIgnoreCase)
                .ToList()
            : Array.Empty<string>();

    public void Delete(string slot)
    {
        var path = PathFor(slot);
        if (File.Exists(path))
        {
            File.Delete(path);
        }
    }

    private string PathFor(string slot)
    {
        foreach (var c in Path.GetInvalidFileNameChars())
        {
            slot = slot.Replace(c, '_');
        }

        return Path.Combine(_root, slot + Extension);
    }
}
