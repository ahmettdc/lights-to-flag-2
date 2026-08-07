using System.IO;
using System.Linq;
using LTF.App.Services;
using LTF.App.Session;
using LTF.App.Settings;
using LTF.App.ViewModels;
using Xunit;

namespace LTF.App.Tests;

/// <summary>
/// The season-boundary rollover (M21f): Continue past the final round rolls the world into a fresh season
/// (reusing the M18 world-sweep chain), the next season runs on the rolled carset, the rolled carset survives
/// a save/reload, and the top-bar Continue crosses the boundary because the career is endless.
/// </summary>
public class Phase3RolloverTests
{
    private static void DriveToSeasonEnd(LiveCareer live)
    {
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
    }

    [Fact]
    public void Continue_at_the_season_boundary_rolls_into_a_fresh_season()
    {
        var session = SessionLoader.LoadFlagship();
        var live = new LiveCareer(session);

        DriveToSeasonEnd(live);
        Assert.True(live.SeasonComplete);
        Assert.Equal(session.Carset.Calendar.Count, live.Results.Count);
        Assert.True(live.Standings.Drivers.Sum(d => d.Points) > 0);

        // One more Continue crosses the boundary: the world rolls over and a new season opens.
        live.Continue();

        Assert.False(live.SeasonComplete);
        Assert.Empty(live.Results); // results cleared for the new season
        Assert.Equal(0, live.Standings.Drivers.Sum(d => d.Points)); // zeroed table
        Assert.Contains(live.LastStepNews, n => n.Id.StartsWith("season-"));
    }

    [Fact]
    public void The_next_season_runs_to_completion_on_the_rolled_carset()
    {
        var session = SessionLoader.LoadFlagship();
        var live = new LiveCareer(session);

        DriveToSeasonEnd(live);
        live.Continue(); // roll into season two
        var rolled = live.SeasonStart;
        Assert.NotSame(session.Carset, rolled);

        DriveToSeasonEnd(live); // play season two on the rolled carset
        Assert.True(live.SeasonComplete);
        Assert.Equal(rolled.Calendar.Count, live.Results.Count);
        Assert.True(live.Standings.Drivers.Sum(d => d.Points) > 0);
    }

    [Fact]
    public void A_rolled_carset_survives_a_save_and_reload()
    {
        var catalog = CarsetCatalog.Discover();
        var dir = Directory.CreateTempSubdirectory().FullName;
        var saves = new SaveStore(catalog, dir);

        var session = SessionLoader.LoadFlagship();
        var live = new LiveCareer(session);
        DriveToSeasonEnd(live);
        live.Continue(); // into season two
        for (var i = 0; i < 4 && !live.SeasonComplete; i++)
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

        saves.Save(live.SaveSession);
        var reloaded = saves.Load(saves.MostRecent()!);
        var resumed = new LiveCareer(reloaded);

        // The reload carries the rolled roster (not the original season-one line-up) and reconstructs the same
        // number of completed rounds — so the save persisted the evolved carset, not the pristine one.
        Assert.Equal(
            live.SeasonStart.Drivers.Select(d => d.Id).OrderBy(x => x, System.StringComparer.Ordinal),
            reloaded.Carset.Drivers.Select(d => d.Id).OrderBy(x => x, System.StringComparer.Ordinal));
        Assert.Equal(live.Results.Count, resumed.Results.Count);
    }

    [Fact]
    public void The_top_bar_continue_stays_enabled_across_the_boundary()
    {
        var catalog = CarsetCatalog.Discover();
        var dir = Directory.CreateTempSubdirectory().FullName;
        var saves = new SaveStore(catalog, dir);
        var settings = new SettingsStore(Path.Combine(dir, "settings.json"));
        var root = new RootViewModel(new AppServices(catalog, saves, settings, new CareerNotificationSource()));

        root.EnterCareer(SessionLoader.LoadFlagship());
        var shell = (ShellViewModel)root.Content!;

        // Enough continues to finish season one and cross into season two.
        for (var i = 0; i < 40; i++)
        {
            shell.TopBar.ContinueCommand.Execute(null);
        }

        Assert.True(shell.TopBar.CanContinue); // the career is endless — the button never disables at a boundary
        Assert.Contains(shell.Inbox.Items, it => it.Model.Id.StartsWith("season-"));
    }
}
