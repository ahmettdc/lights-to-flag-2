using System;
using System.Linq;
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
using LTF.Domain;

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
    private LiveCareer? _live;
    private NavigationService? _navigation;
    private ShellViewModel? _shell;

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
        // The live career reconstructs its standings from (season-start carset, seed, date), so a loaded
        // mid-season save shows the correct table without any change to the save format. Autosave the
        // season-start carset + date so Continue/Load see it.
        var live = new LiveCareer(session);
        _live = live;
        _services.Saves.Save(live.SaveSession);

        // Start the career's inbox feed fresh; Continue appends dated items to it (M21e).
        if (_services.Notifications is IMutableNotificationSource feed)
        {
            feed.Reset();
        }

        var navigation = new NavigationService();
        _navigation = navigation;
        navigation.Register(NavKey.Settings, () => new SettingsViewModel(
            _services.Settings.Load(),
            _services.Settings,
            onClose: () => navigation.Navigate(NavKey.PaddockHub)));

        // In-shell career screens (M21). The factories close over the live career, so re-navigating after a
        // Continue rebuilds each screen from current state (evolving carset, advancing clock, live standings).
        navigation.Register(NavKey.PaddockHub, () => new PaddockHubViewModel(
            live.Session,
            live.Standings,
            _services.Notifications,
            goToRaceWeekend: () => navigation.Navigate(NavKey.Calendar)));
        navigation.Register(NavKey.Standings, () => new StandingsViewModel(live.Current, live.Standings));
        navigation.Register(NavKey.Calendar, () => new CalendarViewModel(live.Current, live.Clock));
        navigation.Register(NavKey.Drivers, () => new DriversViewModel(live.Current));
        navigation.Register(NavKey.Database, () => new DatabaseViewModel(live.Current));

        // Management screens (M22). Read-only projections over the live career; a Continue rebuilds them.
        // Finance also carries the first player mutation — taking a loan (M22c).
        navigation.Register(NavKey.Finance, () => new FinanceViewModel(live.Current, live.Standings, borrow: BorrowLoan));
        navigation.Register(NavKey.RndFacilities, () => new RndFacilitiesViewModel(live.Current));
        navigation.Register(NavKey.CarsPowerUnit, () => new CarsPowerUnitViewModel(live.Current));

        // The career is endless (Continue rolls into the next season at a boundary) and the button also
        // acknowledges a Rev-15 pause, so it stays enabled unless an action is pending; ContinueCareerStep
        // decides whether a click advances, rolls over or acknowledges.
        var shell = new ShellViewModel(
            navigation, new SessionSnapshot(live.Session), _services.Notifications,
            host: this, onContinue: ContinueCareerStep, canContinue: () => live.CanContinue);
        _shell = shell;
        Content = shell;
    }

    // One Football-Manager "Continue": if the career is paused on an action-required item, this click
    // acknowledges it (Rev 15); otherwise it advances the career, appends the step's dated news to the inbox
    // and autosaves. Either way it refreshes the top bar and re-renders the open screen from the new state.
    private void ContinueCareerStep()
    {
        if (_live is null)
        {
            return;
        }

        if (_live.PendingAction)
        {
            _live.Acknowledge();
        }
        else
        {
            _live.Continue();

            if (_services.Notifications is IMutableNotificationSource feed)
            {
                foreach (var item in _live.LastStepNews)
                {
                    feed.Append(item);
                }
            }

            if (_services.Settings.Load().Autosave)
            {
                _services.Saves.Save(_live.SaveSession);
            }
        }

        RefreshOpenScreen();
    }

    // Take a bank loan for the player team (M22c) — the first in-shell player mutation. The draw lands on the
    // season-start carset (the save/reconstruction base) via LiveCareer.ApplyToSeasonStart, freezing the
    // offered rate into the loan, then autosaves + rebuilds the open screen. A declined draw leaves the carset
    // unchanged (BankLedger.Borrow returns the original team).
    private void BorrowLoan(long amount, int termSeasons)
    {
        if (_live is null)
        {
            return;
        }

        var standings = _live.Standings;
        _live.ApplyToSeasonStart(carset =>
        {
            var team = carset.PlayerTeam();
            if (team is null)
            {
                return carset;
            }

            var board = carset.Boards.FirstOrDefault(b => string.CompareOrdinal(b.TeamId, carset.PlayerTeamId) == 0);
            var profile = CreditProfile.For(team, standings, board, carset.Rules.Bank, carset.Rules.Economy);
            var loanId = string.Create(System.Globalization.CultureInfo.InvariantCulture, $"loan-{team.Finances.Loans.Count + 1}");
            var result = BankLedger.Borrow(team, profile, amount, termSeasons, loanId);
            if (!result.Approved)
            {
                return carset;
            }

            var teams = carset.Teams
                .Select(t => string.CompareOrdinal(t.Id, team.Id) == 0 ? result.Team : t)
                .ToList();
            return carset with { Teams = teams };
        });

        CommitCareerMutation();
    }

    // Persist a player mutation and re-render: autosave the season-start carset (if enabled) then refresh.
    private void CommitCareerMutation()
    {
        if (_live is not null && _services.Settings.Load().Autosave)
        {
            _services.Saves.Save(_live.SaveSession);
        }

        RefreshOpenScreen();
    }

    // Refresh the top bar and rebuild the open screen from the current live state (the factory closes over the
    // live career, so re-navigating the current key re-projects it).
    private void RefreshOpenScreen()
    {
        if (_live is null)
        {
            return;
        }

        _shell?.TopBar.Refresh(new SessionSnapshot(_live.Session));
        _navigation?.Navigate(_navigation.CurrentKey);
    }

    public void ExitToMenu()
    {
        // Autosave the current career on the way out (if enabled), then drop the shell.
        if (_live is not null && _services.Settings.Load().Autosave)
        {
            _services.Saves.Save(_live.SaveSession);
        }

        _live = null;
        _navigation = null;
        _shell = null;
        ShowMainMenu();
    }

    public void Quit() => _quit?.Invoke();
}
