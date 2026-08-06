using LTF.App.Session;

namespace LTF.App.ViewModels.Menu;

/// <summary>One row in the load-game list, wrapping a discovered <see cref="SaveSlot"/> for display.</summary>
public sealed class SaveSlotViewModel
{
    public SaveSlotViewModel(SaveSlot slot) => Slot = slot;

    public SaveSlot Slot { get; }

    public string TeamName => Slot.TeamName;

    public string TeamBadge => Slot.TeamBadge;

    public string Date => Slot.Date.ToString("d MMM yyyy");

    public string LastSaved => $"saved {Slot.LastSavedUtc.ToLocalTime():d MMM yyyy · HH:mm}";
}
