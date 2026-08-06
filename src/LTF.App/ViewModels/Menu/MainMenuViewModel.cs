using CommunityToolkit.Mvvm.Input;
using LTF.App.Mvvm;
using LTF.App.Services;

namespace LTF.App.ViewModels.Menu;

/// <summary>
/// The main menu — the app's landing screen (design/mockups/ui.dc.html). Every command delegates to the
/// <see cref="IAppShellController"/> so the menu never touches a window or builds a session itself.
/// <see cref="CanContinue"/> gates the Continue button (there is no save store until M20d, so it starts
/// off). New Career / Load Game / Quick Race / Settings become live as their screens land across M20.
/// </summary>
public sealed partial class MainMenuViewModel : ViewModelBase
{
    private readonly IAppShellController _host;

    public MainMenuViewModel(IAppShellController host, bool canContinue)
    {
        _host = host;
        CanContinue = canContinue;
    }

    /// <summary>Whether a career exists to resume. Set once at construction (fixed for this menu's life).</summary>
    public bool CanContinue { get; }

    [RelayCommand]
    private void NewCareer() => _host.ShowNewCareer();

    [RelayCommand(CanExecute = nameof(CanContinue))]
    private void Continue() => _host.ContinueCareer();

    [RelayCommand]
    private void LoadGame() => _host.ShowLoadGame();

    [RelayCommand]
    private void QuickRace() => _host.ShowQuickRace();

    [RelayCommand]
    private void Settings() => _host.ShowSettings();

    [RelayCommand]
    private void Quit() => _host.Quit();
}
