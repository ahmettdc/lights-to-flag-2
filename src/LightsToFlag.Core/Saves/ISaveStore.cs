using LightsToFlag.Core.Career;

namespace LightsToFlag.Core.Saves;

/// <summary>Persists and retrieves career saves by slot name.</summary>
public interface ISaveStore
{
    void Save(string slot, CareerState state);
    CareerState Load(string slot);
    bool Exists(string slot);
    IReadOnlyList<string> ListSlots();
    void Delete(string slot);
}
