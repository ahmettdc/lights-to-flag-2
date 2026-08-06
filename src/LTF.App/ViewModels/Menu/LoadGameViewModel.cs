using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LTF.App.Mvvm;
using LTF.App.Services;
using LTF.App.Session;

namespace LTF.App.ViewModels.Menu;

/// <summary>
/// The load-game screen (M20d): lists saved careers newest-first and loads or deletes the selected one.
/// Reads the save store on construction and after a delete; empty when no saves exist.
/// </summary>
public sealed partial class LoadGameViewModel : ViewModelBase
{
    private readonly IAppShellController _host;
    private readonly SaveStore _saves;

    public LoadGameViewModel(IAppShellController host, SaveStore saves)
    {
        _host = host;
        _saves = saves;
        Refresh();
    }

    public ObservableCollection<SaveSlotViewModel> Slots { get; } = new();

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(LoadCommand), nameof(DeleteCommand))]
    private SaveSlotViewModel? _selectedSlot;

    public bool IsEmpty => Slots.Count == 0;

    private bool HasSelection => SelectedSlot is not null;

    [RelayCommand(CanExecute = nameof(HasSelection))]
    private void Load() => _host.EnterCareer(_saves.Load(SelectedSlot!.Slot));

    [RelayCommand(CanExecute = nameof(HasSelection))]
    private void Delete()
    {
        _saves.Delete(SelectedSlot!.Slot);
        Refresh();
    }

    [RelayCommand]
    private void Back() => _host.ShowMainMenu();

    private void Refresh()
    {
        Slots.Clear();
        foreach (var slot in _saves.List())
        {
            Slots.Add(new SaveSlotViewModel(slot));
        }

        SelectedSlot = Slots.FirstOrDefault();
        OnPropertyChanged(nameof(IsEmpty));
    }
}
