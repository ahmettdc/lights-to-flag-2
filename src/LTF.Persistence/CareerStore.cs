using System.Text;
using System.Text.Json;
using LTF.Career;

namespace LTF.Persistence;

/// <summary>
/// Reads and writes career saves as JSON (M11e). This is the only layer that touches the disk, and
/// everything it serializes is a plain <see cref="CareerState"/> snapshot. Serialization is
/// deterministic — the writer emits properties in a stable order with invariant number and date
/// formatting and line-feed indentation, independent of the host's culture or OS — so the same state
/// always produces byte-for-byte identical files and a save survives a round trip unchanged.
/// </summary>
public static class CareerStore
{
    private static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = true,
    };

    // No byte-order mark: the file is pure UTF-8 so its bytes match across platforms.
    private static readonly UTF8Encoding Utf8NoBom = new(encoderShouldEmitUTF8Identifier: false);

    /// <summary>Serialize a career state to its JSON form (no disk access).</summary>
    public static string Serialize(CareerState state) =>
        JsonSerializer.Serialize(state, Options);

    /// <summary>Parse a career state from its JSON form.</summary>
    public static CareerState Deserialize(string json) =>
        JsonSerializer.Deserialize<CareerState>(json, Options)
        ?? throw new InvalidDataException("career save was empty.");

    /// <summary>Write a career save to <paramref name="path"/> (UTF-8, no BOM, trailing newline).</summary>
    public static void Save(CareerState state, string path)
    {
        var json = Serialize(state) + "\n";
        File.WriteAllText(path, json, Utf8NoBom);
    }

    /// <summary>Read a career save back from <paramref name="path"/>.</summary>
    public static CareerState Load(string path) =>
        Deserialize(File.ReadAllText(path));
}
