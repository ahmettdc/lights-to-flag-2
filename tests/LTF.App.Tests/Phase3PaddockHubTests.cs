using System.IO;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Threading;
using Avalonia.VisualTree;
using LTF.App.Navigation;
using LTF.App.Services;
using LTF.App.Session;
using LTF.App.Settings;
using LTF.App.ViewModels;
using LTF.App.ViewModels.Screens;
using LTF.App.Views.Screens;
using LTF.Career;
using Xunit;

namespace LTF.App.Tests;

/// <summary>
/// The paddock hub dashboard (M21c): aggregates the snapshot, clock, standings and notifications into the
/// summary column + current-events snippet, exposes the "go to race weekend" deep link, resolves headless,
/// and is the live landing screen once a career is entered.
/// </summary>
public class Phase3PaddockHubTests
{
    private static PaddockHubViewModel Make(out bool[] wentToRaceWeekend, ShellSession? session = null)
    {
        var s = session ?? SessionLoader.LoadFlagship();
        var flag = new bool[1];
        wentToRaceWeekend = flag;
        return new PaddockHubViewModel(
            s,
            ChampionshipStandings.Empty(s.Carset),
            new SampleNotificationSource(),
            goToRaceWeekend: () => flag[0] = true);
    }

    [Fact]
    public void Hub_summarises_the_season_and_the_inbox()
    {
        var vm = Make(out _);

        Assert.NotEqual("—", vm.WccText); // the flagship has a player team, so there is a WCC line
        Assert.True(vm.HasNextRace);
        Assert.NotEmpty(vm.NextEventName);

        // The sample source ships 1 critical / 1 warning / 3 info+success items.
        Assert.Equal(1, vm.CriticalCount);
        Assert.Equal(1, vm.WarningCount);
        Assert.Equal(3, vm.PendingCount);
        Assert.Equal(5, vm.Events.Count);
        Assert.Equal("INBOX · 5 NEW", vm.InboxCountText);
    }

    [Fact]
    public void Hub_go_to_race_weekend_invokes_the_deep_link()
    {
        var vm = Make(out var went);

        vm.GoToRaceWeekendCommand.Execute(null);

        Assert.True(went[0]);
    }

    [Fact]
    public void Hub_past_the_final_round_shows_the_season_complete()
    {
        var flagship = SessionLoader.LoadFlagship();
        var lastDate = flagship.Carset.Calendar.Max(r => r.Date);
        var ended = flagship with { Clock = flagship.Clock with { Date = lastDate.AddDays(1) } };

        var vm = Make(out _, ended);

        Assert.False(vm.HasNextRace);
        Assert.Equal("SEASON COMPLETE", vm.NextEventName);
    }

    [AvaloniaFact]
    public void Hub_view_resolves_from_its_view_model()
    {
        var host = new ContentControl { Content = Make(out _) };
        var window = new Window { Content = host };

        window.Show();
        Dispatcher.UIThread.RunJobs();

        Assert.Single(host.GetVisualDescendants().OfType<PaddockHubView>());
    }

    [Fact]
    public void Entering_a_career_lands_on_the_live_paddock_hub()
    {
        var catalog = CarsetCatalog.Discover();
        var dir = Directory.CreateTempSubdirectory().FullName;
        var saves = new SaveStore(catalog, dir);
        var settings = new SettingsStore(Path.Combine(dir, "settings.json"));
        var root = new RootViewModel(new AppServices(catalog, saves, settings, new SampleNotificationSource()));

        root.EnterCareer(SessionLoader.LoadFlagship());
        var shell = (ShellViewModel)root.Content!;

        Assert.Equal(NavKey.PaddockHub, shell.Navigation.CurrentKey);
        Assert.IsType<PaddockHubViewModel>(shell.Navigation.CurrentScreen);
    }
}
