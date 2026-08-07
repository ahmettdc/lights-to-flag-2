using System.IO;
using System.Linq;
using LTF.App.Navigation;
using LTF.App.Services;
using LTF.App.Session;
using LTF.App.Settings;
using LTF.App.ViewModels;
using LTF.App.ViewModels.Screens;
using LTF.Career;
using Xunit;

namespace LTF.App.Tests;

/// <summary>
/// The live career + per-event Continue (M21d): Continue advances the clock and runs the due round, a full
/// season of Continues reproduces the whole-season simulation, a mid-season date reconstructs the same
/// standings deterministically, and the top-bar Continue drives it all through the shell.
/// </summary>
public class Phase3LiveCareerTests
{
    private static RootViewModel MakeRoot()
    {
        var catalog = CarsetCatalog.Discover();
        var dir = Directory.CreateTempSubdirectory().FullName;
        var saves = new SaveStore(catalog, dir);
        var settings = new SettingsStore(Path.Combine(dir, "settings.json"));
        return new RootViewModel(new AppServices(catalog, saves, settings, new SampleNotificationSource()));
    }

    [Fact]
    public void Continue_advances_the_clock_and_a_new_career_starts_with_no_results()
    {
        var live = new LiveCareer(SessionLoader.LoadFlagship());

        Assert.Empty(live.Results); // the day before the first event — nothing has run
        var before = live.Clock.Date;

        live.Continue();

        Assert.True(live.Clock.Date > before); // Continue always jumps to the next dated event
    }

    [Fact]
    public void A_full_season_of_continues_runs_every_round_and_matches_the_whole_season_sim()
    {
        var session = SessionLoader.LoadFlagship();
        var live = new LiveCareer(session);

        var guard = 0;
        while (!live.SeasonComplete && guard++ < 1000)
        {
            if (live.PendingAction)
            {
                live.Acknowledge();
            }
            else
            {
                live.Continue();
            }
        }

        Assert.True(live.SeasonComplete);
        Assert.Equal(session.Carset.Calendar.Count, live.Results.Count);

        // The live career, run round-by-round, reproduces the one-shot whole-season simulation exactly.
        var expected = SeasonSimulator.Run(session.Carset, session.Seed).Standings;
        Assert.Equal(expected.Drivers, live.Standings.Drivers);
        Assert.Equal(expected.Constructors, live.Standings.Constructors);
        Assert.True(live.Standings.Drivers.Sum(d => d.Points) > 0);
    }

    [Fact]
    public void A_mid_season_date_reconstructs_the_same_standings_as_advancing_there_live()
    {
        var session = SessionLoader.LoadFlagship();

        var live = new LiveCareer(session);
        for (var i = 0; i < 8 && live.CanContinue; i++)
        {
            live.Continue();
        }

        // Rebuild from (season-start carset, seed, the reached date) — the load path — with no persisted state.
        var resumed = new LiveCareer(session with { Clock = session.Clock with { Date = live.Clock.Date } });

        Assert.Equal(live.Results.Count, resumed.Results.Count);
        Assert.Equal(live.Standings.Drivers, resumed.Standings.Drivers);
        Assert.Equal(live.Standings.Constructors, resumed.Standings.Constructors);
    }

    [Fact]
    public void Top_bar_continue_drives_the_season_and_refreshes_the_shell()
    {
        var root = MakeRoot();
        root.EnterCareer(SessionLoader.LoadFlagship());
        var shell = (ShellViewModel)root.Content!;

        var beforeDate = shell.TopBar.DateText;

        // A dozen continues run several rounds without leaving the first season.
        for (var i = 0; i < 12; i++)
        {
            shell.TopBar.ContinueCommand.Execute(null);
        }

        Assert.NotEqual(beforeDate, shell.TopBar.DateText); // the top bar re-read the advanced clock

        // The open screen rebuilds from live state: the standings now carry the season's points so far.
        shell.Navigation.Navigate(NavKey.Standings);
        var standings = (StandingsViewModel)shell.Navigation.CurrentScreen!;
        Assert.True(standings.Drivers.Sum(d => d.Points) > 0);
    }
}
