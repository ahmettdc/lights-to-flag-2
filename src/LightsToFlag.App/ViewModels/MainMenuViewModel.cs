using System.Threading.Tasks;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LightsToFlag.App.Services;

namespace LightsToFlag.App.ViewModels;

/// <summary>The front-page menu: new career, continue, load, about, quit.</summary>
public partial class MainMenuViewModel : ObservableObject
{
    private readonly INavigationService _nav;
    private readonly GameSession _session;
    private readonly UpdateService _updates;

    public MainMenuViewModel(INavigationService nav, GameSession session, UpdateService updates)
    {
        _nav = nav;
        _session = session;
        _updates = updates;
    }

    public bool CanContinue => _session.HasCareer;
    public bool HasSaves => _session.SaveSlots().Count > 0;

    [RelayCommand]
    private void NewCareer() => _nav.NavigateTo<NewCareerViewModel>();

    [RelayCommand]
    private void Continue()
    {
        if (_session.HasCareer)
        {
            _nav.NavigateTo<CareerViewModel>();
        }
    }

    [RelayCommand]
    private void Load() => _nav.NavigateTo<LoadGameViewModel>();

    [RelayCommand]
    private void About() => _nav.NavigateTo<AboutViewModel>();

    [RelayCommand]
    private void Quit() => Application.Current.Shutdown();

    [RelayCommand]
    private async Task CheckUpdates() => await _updates.CheckAndReportAsync();
}
