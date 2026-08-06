using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using LTF.Career;
using LTF.Persistence;

namespace LTF.App.Session;

/// <summary>
/// Save slots on top of the deterministic <see cref="CareerStore"/> (which takes a plain path — slots are the
/// UI's job). A slot's carset id lives in its filename (<c>{carsetId}__{slot}.json</c>) because a
/// <c>CareerState</c> carries none, and "last saved" is the file's timestamp — display-only metadata, never
/// game state — so there is no second serializer and no <c>CareerState</c> change. Load re-reads the pristine
/// carset from the catalog and overlays the saved state, restoring the saved date via
/// <see cref="ShellSession.Resume"/> (not the season-start default).
/// </summary>
public sealed class SaveStore
{
    private const string Separator = "__";
    private const string AutoSlot = "autosave";

    private readonly CarsetCatalog _catalog;
    private readonly string _dir;

    public SaveStore(CarsetCatalog catalog, string savesDir)
    {
        _catalog = catalog;
        _dir = savesDir;
    }

    /// <summary>Every save on disk, most recently written first.</summary>
    public IReadOnlyList<SaveSlot> List()
    {
        if (!Directory.Exists(_dir))
        {
            return [];
        }

        var slots = new List<SaveSlot>();
        foreach (var path in Directory.EnumerateFiles(_dir, "*.json"))
        {
            var slot = Describe(path);
            if (slot is not null)
            {
                slots.Add(slot);
            }
        }

        return slots.OrderByDescending(s => s.LastSavedUtc).ToList();
    }

    /// <summary>Write the session to a slot (autosave by default), creating the saves folder if needed.</summary>
    public void Save(ShellSession session, string slot = AutoSlot)
    {
        Directory.CreateDirectory(_dir);
        var state = CareerState.Capture(session.Carset, session.Clock.Date, session.Seed);
        CareerStore.Save(state, PathFor(session.Carset.Id, slot));
    }

    /// <summary>Load a slot back into a session, restoring its saved date.</summary>
    public ShellSession Load(SaveSlot slot)
    {
        var descriptor = _catalog.Find(slot.CarsetId)
            ?? throw new InvalidDataException($"save references unknown carset '{slot.CarsetId}'.");
        var state = CareerStore.Load(slot.SavePath);
        var resumed = state.RestoreInto(descriptor.Carset);
        return ShellSession.Resume(resumed, state.Seed, state.Date);
    }

    public bool HasAnySave() => Directory.Exists(_dir) && Directory.EnumerateFiles(_dir, "*.json").Any();

    public SaveSlot? MostRecent() => List().FirstOrDefault();

    public void Delete(SaveSlot slot)
    {
        if (File.Exists(slot.SavePath))
        {
            File.Delete(slot.SavePath);
        }
    }

    private string PathFor(string carsetId, string slot) =>
        Path.Combine(_dir, $"{carsetId}{Separator}{slot}.json");

    private SaveSlot? Describe(string path)
    {
        var name = Path.GetFileNameWithoutExtension(path);
        var split = name.IndexOf(Separator, StringComparison.Ordinal);
        if (split <= 0)
        {
            return null;
        }

        var carsetId = name[..split];
        var slot = name[(split + Separator.Length)..];

        CareerState state;
        try
        {
            state = CareerStore.Load(path);
        }
        catch (Exception)
        {
            return null; // skip a corrupt save rather than crash the list
        }

        var (teamName, teamBadge) = ResolveTeam(carsetId, state.PlayerTeamId);
        return new SaveSlot(carsetId, slot, path, teamName, teamBadge, state.Date, File.GetLastWriteTimeUtc(path));
    }

    private (string Name, string Badge) ResolveTeam(string carsetId, string teamId)
    {
        var descriptor = _catalog.Find(carsetId);
        if (descriptor is null)
        {
            return (carsetId, "");
        }

        if (teamId.Length == 0)
        {
            return (descriptor.Summary.Name, "");
        }

        var team = descriptor.Carset.Teams.FirstOrDefault(t => string.CompareOrdinal(t.Id, teamId) == 0);
        return team is null ? (descriptor.Summary.Name, "") : (team.Name, team.ShortName);
    }
}
