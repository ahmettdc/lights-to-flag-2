using System;
using CommunityToolkit.Mvvm.ComponentModel;
using LTF.App.Mvvm;
using LTF.App.Navigation;
using LTF.App.Services;
using LTF.App.Session;
using LTF.App.ViewModels.Menu;
using LTF.App.ViewModels.Quick;
using LTF.App.ViewModels.Screens;
using LTF.App.ViewModels.Settings;
using LTF.Career;

namespace LTF.App.ViewModels;

/// <summary>
/// The application root. Holds the single <see cref="Content"/> the window shows and swaps it between the
/// menu layer (main menu, new-career wizard, load-game, and — in later phases — settings and Quick Race) and
/// the in-game <see cref="ShellViewModel"/>. Menu view-models never touch a <c>Window</c> — they call back
/// through <see cref="IAppShellController"/>, which this implements. Built once at startup by <see cref="App"/>
/// (over the real stores) or directly in headless tests (over temp stores). Entering a career autosaves it,
/// so Continue and Load see it; loading restores the saved date via <see cref="SaveStore.Load"/>.
/// </summary>
public sealed partial class RootViewModel : ViewModelBase, IAppShellController
{
    private readonly AppServices _services;
    private readonly Action? _quit;
    private ShellSession? _currentSession;

    public RootViewModel(AppServices services, Action? quit = null)
    {
        _services = services;
        _quit = quit;
        ShowMainMenu();
    }

    /// <summary>The screen the window currently shows — a menu view-model or the in-game shell.</summary>
    [ObservableProperty]
    private object? _content;

    public void ShowMainMenu() => Content = new MainMenuViewModel(this, _services.Saves.HasAnySave());

    public void ShowNewCareer() => Content = new NewCareerViewModel(this, _services.Catalog);

    public void ContinueCareer()
    {
        var slot = _services.Saves.MostRecent();
        if (slot is not null)
        {
            EnterCareer(_services.Saves.Load(slot));
        }
    }

    public void ShowLoadGame() => Content = new LoadGameViewModel(this, _services.Saves);

    public void ShowQuickRace() => Content = new QuickRaceViewModel(this, _services.Catalog);

    public void ShowSettings() =>
        Content = new SettingsViewModel(_services.Settings.Load(), _services.Settings, onClose: ShowMainMenu);

    public void EnterCareer(ShellSession session)
    {
        _services.Saves.Save(session);
        _currentSession = session;

        var snapshot = new SessionSnapshot(session);
        var navigation = new NavigationService();
        navigation.Register(NavKey.Settings, () => new SettingsViewModel(
            _services.Settings.Load(),
            _services.Settings,
            onClose: () => navigation.Navigate(NavKey.PaddockHub)));

        // In-shell career screens (M21). The factories close over the session's carset/clock, so
        // re-navigating after a Continue rebuilds each screen from current state. Standings shows the
        // zeroed table until a round is run (the live career wires results in M21d).
        navigation.Register(NavKey.Standings, () =>
            new StandingsViewModel(session.Carset, ChampionshipStandings.Empty(session.Carset)));
        navigation.Register(NavKey.Calendar, () =>
            new CalendarViewModel(session.Carset, session.Clock));

        Content = new ShellViewModel(navigation, snapshot, _services.Notifications, host: this);
    }

    public void ExitToMenu()
    {
        // Autosave the current session on the way out (if enabled), then drop the shell.
        if (_currentSession is not null && _services.Settings.Load().Autosave)
        {
            _services.Saves.Save(_currentSession);
        }

        _currentSession = null;
        ShowMainMenu();
    }

    public void Quit() => _quit?.Invoke();
}
