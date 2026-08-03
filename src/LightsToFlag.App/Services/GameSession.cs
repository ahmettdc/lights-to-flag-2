using System.Collections.Generic;
using LightsToFlag.Core.Career;
using LightsToFlag.Core.Data;
using LightsToFlag.Core.Domain;
using LightsToFlag.Core.Saves;

namespace LightsToFlag.App.Services;

/// <summary>
/// Central, app-wide game state and operations: the loaded carset, the active
/// career, and save/load. View models read and mutate the career through here so
/// there is a single source of truth.
/// </summary>
public sealed class GameSession
{
    private readonly ICarsetLoader _loader;
    private readonly ISaveStore _saveStore;
    private readonly CarsetCatalog _catalog;

    public GameSession(ICarsetLoader loader, ISaveStore saveStore, CarsetCatalog catalog, CareerEngine engine)
    {
        _loader = loader;
        _saveStore = saveStore;
        _catalog = catalog;
        Engine = engine;
    }

    public CareerEngine Engine { get; }

    public Carset? Carset { get; private set; }
    public CareerState? Career { get; set; }

    public bool HasCareer => Carset is not null && Career is not null;

    public IReadOnlyList<CarsetInfo> AvailableCarsets() => _catalog.List();

    public Carset LoadCarset(CarsetInfo info) => _loader.Load(info.Path);

    public void StartNewCareer(CarsetInfo info, string? playerId, int seed)
    {
        Carset = _loader.Load(info.Path);
        Career = Engine.Start(Carset, playerId, seed);
    }

    public IReadOnlyList<string> SaveSlots() => _saveStore.ListSlots();

    public bool SlotExists(string slot) => _saveStore.Exists(slot);

    public void Save(string slot)
    {
        if (Career is not null)
        {
            _saveStore.Save(slot, Career);
        }
    }

    public void Load(string slot)
    {
        var state = _saveStore.Load(slot);
        var info = _catalog.FindByName(state.CarsetName)
                   ?? throw new CarsetValidationException(
                       $"Save references carset '{state.CarsetName}', which is not installed.");
        Carset = _loader.Load(info.Path);
        Career = state;
    }

    public bool SeasonComplete =>
        Carset is not null && Career is not null && Engine.IsSeasonComplete(Career, Carset);
}
