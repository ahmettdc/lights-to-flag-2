using System.Globalization;
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
using Xunit;

namespace LTF.App.Tests;

/// <summary>
/// The records &amp; statistics screen (M24): career profiles ranked by their accumulated record, plus the enriched
/// driver/team profile card. A read-only projection over the live career, so the golden race is untouched. Also
/// covers the shell registration and a headless view resolve.
/// </summary>
public class Phase5RecordsTests
{
    // Drive a fresh flagship career until its first season has rolled over (so careers are non-zero and the
    // season archive is populated), acknowledging any halts along the way.
    private static LiveCareer AfterFirstSeason()
    {
        var live = new LiveCareer(SessionLoader.LoadFlagship());
        var guard = 0;
        while (live.Current.SeasonHistory.Count == 0 && guard++ < 5000)
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

        return live;
    }

    [Fact]
    public void The_profiles_tab_lists_every_driver_ranked()
    {
        var live = new LiveCareer(SessionLoader.LoadFlagship());
        var vm = new RecordsViewModel(live);

        Assert.Equal(live.Current.Drivers.Count, vm.Profiles.Count);
        for (var i = 0; i < vm.Profiles.Count; i++)
        {
            Assert.Equal((i + 1).ToString(CultureInfo.InvariantCulture), vm.Profiles[i].Rank);
            Assert.False(string.IsNullOrWhiteSpace(vm.Profiles[i].Name));
        }

        Assert.NotNull(vm.ProfileDetail);      // the first row is selected by default
        Assert.True(vm.ProfileDetail!.HasCareer);
    }

    [Fact]
    public void The_profiles_are_ordered_by_career_points_after_a_season()
    {
        var vm = new RecordsViewModel(AfterFirstSeason());

        var points = vm.Profiles
            .Select(p => double.Parse(p.Points, CultureInfo.InvariantCulture))
            .ToList();
        for (var i = 1; i < points.Count; i++)
        {
            Assert.True(points[i] <= points[i - 1]); // ranked best-first
        }

        Assert.Contains(vm.Profiles, p => p.Races != "0");   // a season's worth of races recorded
        Assert.Equal("1", vm.Profiles[0].Titles);            // the points leader took the title
    }

    [Fact]
    public void Selecting_a_profile_swaps_the_detail_card()
    {
        var vm = new RecordsViewModel(AfterFirstSeason());
        Assert.True(vm.Profiles.Count > 1);

        var first = vm.ProfileDetail;
        vm.SelectedProfile = vm.Profiles[1];
        Assert.NotSame(first, vm.ProfileDetail);
        Assert.True(vm.ProfileDetail!.HasCareer);
        Assert.Contains(vm.ProfileDetail.CareerStats, s => s.Label == "Championships");
    }

    [Fact]
    public void A_team_profile_carries_its_honours()
    {
        var carset = SessionLoader.LoadFlagship().Carset;
        var detail = EntityDetailViewModel.ForTeam(carset.Teams[0], 2, 80);

        Assert.True(detail.HasCareer);
        Assert.Contains(detail.CareerStats, s => s.Label == "Championships");
        Assert.Contains(detail.CareerStats, s => s.Label == "Race wins");
    }

    [AvaloniaFact]
    public void Records_view_resolves_from_its_view_model()
    {
        var host = new ContentControl { Content = new RecordsViewModel(new LiveCareer(SessionLoader.LoadFlagship())) };
        var window = new Window { Content = host };

        window.Show();
        Dispatcher.UIThread.RunJobs();

        Assert.Single(host.GetVisualDescendants().OfType<RecordsView>());
    }

    [Fact]
    public void Entering_a_career_registers_the_records_screen()
    {
        var catalog = CarsetCatalog.Discover();
        var dir = Directory.CreateTempSubdirectory().FullName;
        var saves = new SaveStore(catalog, dir);
        var settings = new SettingsStore(Path.Combine(dir, "settings.json"));
        var root = new RootViewModel(new AppServices(catalog, saves, settings, new CareerNotificationSource()));

        root.EnterCareer(SessionLoader.LoadFlagship());
        var shell = (ShellViewModel)root.Content!;

        shell.Navigation.Navigate(NavKey.Records);
        Assert.IsType<RecordsViewModel>(shell.Navigation.CurrentScreen);
    }

    // --- All-time records + hall of fame (M24e) ---

    [Fact]
    public void The_all_time_boards_rank_each_cumulative_stat()
    {
        var vm = new RecordsViewModel(AfterFirstSeason());

        Assert.NotEmpty(vm.AllTimeBoards);
        Assert.Contains(vm.AllTimeBoards, b => b.Title == "MOST WINS");
        Assert.All(vm.AllTimeBoards, board =>
        {
            Assert.True(board.Entries.Count <= 5); // a top-five leaderboard
            var values = board.Entries.Select(e => double.Parse(e.Value, CultureInfo.InvariantCulture)).ToList();
            for (var i = 1; i < values.Count; i++)
            {
                Assert.True(values[i] <= values[i - 1]); // ranked best-first
            }
        });
    }

    [Fact]
    public void The_hall_of_fame_lists_the_season_champions()
    {
        var live = AfterFirstSeason();
        var vm = new RecordsViewModel(live);

        Assert.True(vm.HasHistory);
        Assert.Equal(live.Current.SeasonHistory.Count, vm.HallOfFame.Count);
        Assert.All(vm.HallOfFame, c =>
        {
            Assert.False(string.IsNullOrWhiteSpace(c.Year));
            Assert.False(string.IsNullOrWhiteSpace(c.DriverChampion));
            Assert.False(string.IsNullOrWhiteSpace(c.ConstructorChampion));
        });

        // Every circuit raced this season carries a lap record, formatted M:SS.mmm.
        Assert.NotEmpty(vm.TrackRecords);
        Assert.All(vm.TrackRecords, t => Assert.Contains(":", t.LapTime));
    }

    [Fact]
    public void A_fresh_career_has_an_empty_hall_of_fame()
    {
        var vm = new RecordsViewModel(new LiveCareer(SessionLoader.LoadFlagship()));

        Assert.False(vm.HasHistory);
        Assert.Empty(vm.HallOfFame);
        Assert.Empty(vm.TrackRecords);
        Assert.NotEmpty(vm.AllTimeBoards); // the boards still list the field, on zero
    }
}
