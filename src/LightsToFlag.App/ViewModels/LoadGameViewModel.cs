using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LightsToFlag.App.Services;

namespace LightsToFlag.App.ViewModels;

/// <summary>Lists save slots and loads one into the session.</summary>
public partial class LoadGameViewModel : ObservableObject
{
    private readonly GameSession _session;
    private readonly INavigationService _nav;

    public LoadGameViewModel(GameSession session, INavigationService nav)
    {
        _session = session;
        _nav = nav;
        foreach (var slot in _session.SaveSlots())
        {
            Slots.Add(slot);
        }

        SelectedSlot = Slots.Count > 0 ? Slots[0] : null;
    }

    public ObservableCollection<string> Slots { get; } = new();

    public bool IsEmpty => Slots.Count == 0;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(LoadCommand))]
    private string? _selectedSlot;

    [ObservableProperty]
    private string? _error;

    private bool CanLoad() => SelectedSlot is not null;

    [RelayCommand(CanExecute = nameof(CanLoad))]
    private void Load()
    {
        if (SelectedSlot is null)
        {
            return;
        }

        try
        {
            _session.Load(SelectedSlot);
            _nav.NavigateTo<CareerViewModel>();
        }
        catch (System.Exception ex)
        {
            Error = ex.Message;
        }
    }

    [RelayCommand]
    private void Back() => _nav.NavigateTo<MainMenuViewModel>();
}
