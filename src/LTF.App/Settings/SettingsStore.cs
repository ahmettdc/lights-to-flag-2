using System;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace LTF.App.Settings;

/// <summary>Reads and writes <see cref="GameSettings"/>.</summary>
public interface ISettingsStore
{
    /// <summary>Load settings, or the defaults when the file is absent or unreadable.</summary>
    GameSettings Load();

    /// <summary>Persist settings.</summary>
    void Save(GameSettings settings);
}

/// <summary>
/// JSON-file settings store, in the same deterministic style as <see cref="LTF.Persistence.CareerStore"/>
/// (indented, UTF-8 no BOM, string enums for a human-editable file). A missing or corrupt file yields the
/// defaults rather than throwing, so a fresh install just works.
/// </summary>
public sealed class SettingsStore : ISettingsStore
{
    private static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() },
    };

    private static readonly UTF8Encoding Utf8NoBom = new(encoderShouldEmitUTF8Identifier: false);

    private readonly string _path;

    public SettingsStore(string path) => _path = path;

    public GameSettings Load()
    {
        try
        {
            if (!File.Exists(_path))
            {
                return GameSettings.Default;
            }

            return JsonSerializer.Deserialize<GameSettings>(File.ReadAllText(_path), Options) ?? GameSettings.Default;
        }
        catch (Exception)
        {
            return GameSettings.Default;
        }
    }

    public void Save(GameSettings settings)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(_path)!);
        File.WriteAllText(_path, JsonSerializer.Serialize(settings, Options) + "\n", Utf8NoBom);
    }
}
