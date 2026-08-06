using System;
using CommunityToolkit.Mvvm.ComponentModel;
using LTF.App.Mvvm;
using LTF.App.Services;
using LTF.App.Session;
using LTF.App.ViewModels.Menu;

namespace LTF.App.ViewModels;

/// <summary>
/// The application root. Holds the single <see cref="Content"/> the window shows and swaps it between the
/// menu layer (the main menu, plus the new-career / load / settings / quick-race screens added across
/// M20) and the in-game <see cref="ShellViewModel"/>. Menu view-models never touch a <c>Window</c> — they
/// call back through <see cref="IAppShellController"/>, which this implements. Built once at startup by
/// <see cref="App"/>, or directly in headless tests. M20a wires the main menu and the menu↔shell switch;
/// the temporary <see cref="ShowNewCareer"/>/<see cref="ContinueCareer"/> bodies load the flagship until
/// the real flows land (M20b/M20d).
/// </summary>
public sealed partial class RootViewModel : ViewModelBase, IAppShellController
{
    private readonly INotificationSource _notifications;
    private readonly Action? _quit;

    public RootViewModel(INotificationSource notifications, Action? quit = null)
    {
        _notifications = notifications;
        _quit = quit;
        ShowMainMenu();
    }

    /// <summary>The screen the window currently shows — a menu view-model or the in-game shell.</summary>
    [ObservableProperty]
    private object? _content;

    public void ShowMainMenu() => Content = new MainMenuViewModel(this, canContinue: false);

    public void ShowNewCareer() =>
        // TODO(M20b): open the carset/team/board wizard. Temporary: start the flagship directly.
        EnterCareer(SessionLoader.LoadFlagship());

    public void ContinueCareer() =>
        // TODO(M20d): resume SaveStore.MostRecent(). Temporary: load the flagship (gated off in the menu).
        EnterCareer(SessionLoader.LoadFlagship());

    public void ShowLoadGame()
    {
        // TODO(M20d): show the load-game slot list.
    }

    public void ShowQuickRace()
    {
        // TODO(M20g): show the Quick Race setup.
    }

    public void ShowSettings()
    {
        // TODO(M20e): show the settings screen.
    }

    public void EnterCareer(ShellSession session)
    {
        var snapshot = new SessionSnapshot(session);
        var navigation = new NavigationService();
        Content = new ShellViewModel(navigation, snapshot, _notifications);
    }

    public void ExitToMenu() => ShowMainMenu();

    public void Quit() => _quit?.Invoke();
}
