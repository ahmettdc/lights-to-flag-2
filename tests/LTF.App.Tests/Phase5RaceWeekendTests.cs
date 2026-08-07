using System.Collections.Generic;
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
using LTF.Domain.Common;
using LTF.Domain.Racing;
using LTF.Simulation.Racing;
using Xunit;

namespace LTF.App.Tests;

/// <summary>
/// The race-weekend screen (M23a): a live timing tower that <em>replays</em> the deterministic race the last
/// career round ran. Pure playback of the recorded telemetry — the projection (running order, gaps, tyres, pit
/// counts, event feed, final classification) is driven by <see cref="RaceWeekendViewModel.SetLap"/>, so the
/// golden race is never re-simulated. Also covers the empty state, the shell registration, and headless resolve.
/// </summary>
public class Phase5RaceWeekendTests
{
    // Drive a fresh flagship career until the first round has actually run (telemetry to replay).
    private static LiveCareer AfterFirstRace()
    {
        var live = new LiveCareer(SessionLoader.LoadFlagship());
        var guard = 0;
        while (live.Results.Count == 0 && guard++ < 1000)
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

    // A stable signature of the tower at the current lap: order + driver + tyre + pit count. Pit counts climb
    // over a race (everyone pits), so lap 1 and the flag always differ — deterministic, never flaky.
    private static string TowerSignature(RaceWeekendViewModel vm) =>
        string.Join("|", vm.Tower.Select(r => $"{r.Position}:{r.Driver}:{r.Tyre}:{r.PitStops}"));

    [Fact]
    public void The_tower_projects_the_running_order_of_the_last_race()
    {
        var vm = new RaceWeekendViewModel(AfterFirstRace());

        Assert.True(vm.HasRace);
        Assert.False(vm.NoRace);
        Assert.True(vm.TotalLaps > 1);
        Assert.NotEmpty(vm.Tower);

        // A well-formed tower: positions 1..N, the leader flagged, everyone named and on a team.
        for (var i = 0; i < vm.Tower.Count; i++)
        {
            Assert.Equal(i + 1, vm.Tower[i].Position);
        }

        Assert.Equal("LEADER", vm.Tower[0].Gap);
        Assert.All(vm.Tower, r => Assert.False(string.IsNullOrWhiteSpace(r.Driver)));

        // The final classification is the full field, points on the podium.
        Assert.NotEmpty(vm.Classification);
        Assert.Contains(vm.Classification, e => e.Points.Length > 0);
    }

    [Fact]
    public void Setting_the_lap_rebuilds_the_tower_for_that_lap()
    {
        var vm = new RaceWeekendViewModel(AfterFirstRace());

        vm.SetLap(1);
        Assert.Equal(1, vm.CurrentLap);
        Assert.False(vm.RaceOver);
        var atStart = TowerSignature(vm);

        vm.SetLap(vm.TotalLaps);
        Assert.Equal(vm.TotalLaps, vm.CurrentLap);
        Assert.True(vm.RaceOver); // the replay reached the flag → the view reveals the classification
        var atFlag = TowerSignature(vm);

        Assert.NotEqual(atStart, atFlag); // the tower reflects the lap it was asked for
    }

    [Fact]
    public void The_lap_counter_clamps_to_the_race_length()
    {
        var vm = new RaceWeekendViewModel(AfterFirstRace());

        vm.SetLap(-5);
        Assert.Equal(1, vm.CurrentLap);

        vm.SetLap(vm.TotalLaps + 999);
        Assert.Equal(vm.TotalLaps, vm.CurrentLap);
    }

    [Fact]
    public void The_event_feed_only_shows_events_up_to_the_current_lap()
    {
        var vm = new RaceWeekendViewModel(AfterFirstRace());

        vm.SetLap(1);
        Assert.All(vm.Feed, e => Assert.True(e.Lap <= 1));
        var early = vm.Feed.Count;

        vm.SetLap(vm.TotalLaps);
        Assert.All(vm.Feed, e => Assert.True(e.Lap <= vm.TotalLaps));
        Assert.True(vm.Feed.Count >= early); // more of the race has unfolded by the flag
    }

    [Fact]
    public void Advancing_past_the_flag_stops_the_replay()
    {
        var vm = new RaceWeekendViewModel(AfterFirstRace());

        vm.SkipToEndCommand.Execute(null);
        Assert.True(vm.RaceOver);
        Assert.False(vm.IsPlaying);

        vm.AdvanceLap(); // at the flag: a no-op that leaves the replay parked, not playing
        Assert.Equal(vm.TotalLaps, vm.CurrentLap);
        Assert.False(vm.IsPlaying);
    }

    [Fact]
    public void Restart_returns_the_replay_to_lap_one()
    {
        var vm = new RaceWeekendViewModel(AfterFirstRace());

        vm.SkipToEndCommand.Execute(null);
        vm.RestartCommand.Execute(null);

        Assert.Equal(1, vm.CurrentLap);
        Assert.False(vm.RaceOver);
        Assert.False(vm.IsPlaying);
    }

    [Fact]
    public void A_career_with_no_race_yet_shows_the_empty_state()
    {
        var live = new LiveCareer(SessionLoader.LoadFlagship());
        Assert.Empty(live.Results); // the day before the first event

        var vm = new RaceWeekendViewModel(live);

        Assert.False(vm.HasRace);
        Assert.True(vm.NoRace);
        Assert.Empty(vm.Tower);
        Assert.Empty(vm.Classification);
        Assert.Equal("", vm.LapText);
    }

    [AvaloniaFact]
    public void Race_weekend_view_resolves_from_its_view_model()
    {
        var host = new ContentControl { Content = new RaceWeekendViewModel(AfterFirstRace()) };
        var window = new Window { Content = host };

        window.Show();
        Dispatcher.UIThread.RunJobs();

        Assert.Single(host.GetVisualDescendants().OfType<RaceWeekendView>());
    }

    [Fact]
    public void Entering_a_career_registers_the_race_weekend_screen()
    {
        var catalog = CarsetCatalog.Discover();
        var dir = Directory.CreateTempSubdirectory().FullName;
        var saves = new SaveStore(catalog, dir);
        var settings = new SettingsStore(Path.Combine(dir, "settings.json"));
        var root = new RootViewModel(new AppServices(catalog, saves, settings, new CareerNotificationSource()));

        root.EnterCareer(SessionLoader.LoadFlagship());
        var shell = (ShellViewModel)root.Content!;

        shell.Navigation.Navigate(NavKey.RaceWeekend);
        Assert.IsType<RaceWeekendViewModel>(shell.Navigation.CurrentScreen);
    }

    // --- Player pre-race strategy (M23b) ---

    [Fact]
    public void The_strategy_panel_lists_the_player_drivers_for_the_upcoming_round()
    {
        var live = new LiveCareer(SessionLoader.LoadFlagship());
        var vm = new RaceWeekendViewModel(live, setStrategy: (_, _, _) => { });

        var team = live.Current.PlayerTeam()!;
        Assert.True(vm.HasUpcoming);
        Assert.Equal(team.DriverIds.Count, vm.Strategy.Count);
        Assert.All(vm.Strategy, s => Assert.True(s.CanEdit));
        Assert.False(string.IsNullOrWhiteSpace(vm.UpcomingText));
    }

    [Fact]
    public void A_read_only_race_weekend_offers_no_strategy_edit_or_start()
    {
        var live = new LiveCareer(SessionLoader.LoadFlagship());
        var vm = new RaceWeekendViewModel(live); // no callbacks

        Assert.All(vm.Strategy, s => Assert.False(s.CanEdit));
        Assert.False(vm.CanStartRace);
    }

    [Fact]
    public void Setting_a_starting_compound_through_the_shell_persists_it()
    {
        var catalog = CarsetCatalog.Discover();
        var dir = Directory.CreateTempSubdirectory().FullName;
        var saves = new SaveStore(catalog, dir);
        var settings = new SettingsStore(Path.Combine(dir, "settings.json"));
        var root = new RootViewModel(new AppServices(catalog, saves, settings, new CareerNotificationSource()));

        root.EnterCareer(SessionLoader.LoadFlagship());
        var shell = (ShellViewModel)root.Content!;
        shell.Navigation.Navigate(NavKey.RaceWeekend);
        var vm = (RaceWeekendViewModel)shell.Navigation.CurrentScreen!;

        Assert.True(vm.HasUpcoming);
        vm.Strategy[0].SelectedCompound = TyreCompound.Hard; // fires the callback → mutation + autosave

        var reloaded = saves.Load(saves.MostRecent()!);
        Assert.Single(reloaded.Carset.PlayerRaceStrategies);
        Assert.Equal(TyreCompound.Hard, reloaded.Carset.PlayerRaceStrategies[0].Compound);
    }

    // --- Qualifying tab (M23d) ---

    [Fact]
    public void The_qualifying_grid_reconstructs_parallel_to_the_results()
    {
        var live = AfterFirstRace();
        Assert.Equal(live.Results.Count, live.Qualifying.Count);
        Assert.NotEmpty(live.Qualifying[0].Grid);

        var vm = new RaceWeekendViewModel(live);
        Assert.NotEmpty(vm.QualifyingGrid);
        for (var i = 0; i < vm.QualifyingGrid.Count; i++)
        {
            Assert.Equal(i + 1, vm.QualifyingGrid[i].GridPosition); // grid in starting order, pole first
            Assert.False(string.IsNullOrWhiteSpace(vm.QualifyingGrid[i].Driver));
        }
    }

    [Fact]
    public void A_resumed_career_reconstructs_the_same_qualifying_grids()
    {
        var session = SessionLoader.LoadFlagship();
        var live = new LiveCareer(session);
        for (var i = 0; i < 8 && live.CanContinue; i++)
        {
            live.Continue();
        }

        // The load path: rebuild from (season-start carset, seed, the reached date), no persisted quali.
        var resumed = new LiveCareer(session with { Clock = session.Clock with { Date = live.Clock.Date } });

        Assert.Equal(live.Qualifying.Count, resumed.Qualifying.Count);
        for (var i = 0; i < live.Qualifying.Count; i++)
        {
            Assert.Equal(
                live.Qualifying[i].Grid.Select(e => (e.GridPosition, e.CompetitorId, e.Part, e.BestLap)),
                resumed.Qualifying[i].Grid.Select(e => (e.GridPosition, e.CompetitorId, e.Part, e.BestLap)));
        }
    }

    [Fact]
    public void A_career_with_no_race_has_an_empty_qualifying_grid()
    {
        var vm = new RaceWeekendViewModel(new LiveCareer(SessionLoader.LoadFlagship()));
        Assert.Empty(vm.QualifyingGrid);
    }

    // --- Track map (M23e) ---

    [Fact]
    public void The_track_map_places_a_marker_per_running_car()
    {
        var vm = new RaceWeekendViewModel(AfterFirstRace());
        vm.SetLap(1);

        Assert.NotEmpty(vm.Markers);
        Assert.Equal(vm.Tower.Count, vm.Markers.Count); // one marker per car in the running order
        Assert.All(vm.Markers, m =>
        {
            Assert.False(double.IsNaN(m.X) || double.IsNaN(m.Y));
            Assert.InRange(m.X, -20.0, 860.0); // within the outline's canvas, with slack for the marker radius
            Assert.InRange(m.Y, -20.0, 580.0);
        });
    }

    [Fact]
    public void The_track_map_markers_shift_as_the_replay_advances()
    {
        var vm = new RaceWeekendViewModel(AfterFirstRace());

        vm.SetLap(1);
        var early = string.Join(";", vm.Markers.Select(m => $"{m.X:0.0},{m.Y:0.0}"));

        vm.SetLap(vm.TotalLaps);
        var late = string.Join(";", vm.Markers.Select(m => $"{m.X:0.0},{m.Y:0.0}"));

        Assert.NotEqual(early, late); // the field spreads differently around the lap as the gaps evolve
    }

    // --- Sector colours + radio (M23f) ---

    [Fact]
    public void The_tower_rows_carry_sector_status_colours()
    {
        var vm = new RaceWeekendViewModel(AfterFirstRace());
        vm.SetLap(1);

        Assert.All(vm.Tower, r =>
        {
            Assert.NotNull(r.S1);
            Assert.NotNull(r.S2);
            Assert.NotNull(r.S3);
        });

        // The session-fastest S1 is coloured differently from a merely personal-best one, so a full grid
        // shows at least two distinct sector colours.
        Assert.True(vm.Tower.Select(r => r.S1).Distinct().Count() >= 2);
    }

    [Fact]
    public void The_radio_feed_voices_the_recorded_events()
    {
        var vm = new RaceWeekendViewModel(AfterFirstRace());
        vm.SetLap(vm.TotalLaps);

        Assert.NotEmpty(vm.Feed);
        Assert.All(vm.Feed, e => Assert.StartsWith("L", e.Text)); // "L{lap} · <voiced message>"
    }

    // --- Interactive live race (M23i) ---

    // A compact, deterministic signature of a race result: the finishing order with laps, times and points.
    // Two byte-identical races share it; a race a pit-wall order changed does not.
    private static string RaceDigest(RaceResult r) =>
        string.Join("|", r.Classification.Select(e =>
            $"{e.Position}:{e.CompetitorId}:{e.Status}:{e.Laps}:{e.TotalTime:R}:{e.Points}"));

    // Advance a career until its first race has run (the round the live screen drives), acknowledging halts.
    private static LiveCareer AdvanceToFirstRace(LiveCareer live)
    {
        var guard = 0;
        while (live.Results.Count == 0 && guard++ < 1000)
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
    public void The_live_stepper_with_no_orders_reproduces_the_plain_race()
    {
        // The round a plain Continue runs (M21d) and the round the live stepper drives with no orders are the
        // same setup with the same seed — so a command-free live race is byte-identical (golden-safe).
        var plain = AfterFirstRace().Results[0];

        var stepper = new LiveCareer(SessionLoader.LoadFlagship()).StartLiveRace();
        Assert.NotNull(stepper);
        while (!stepper!.IsComplete)
        {
            stepper.AdvanceLap();
        }

        Assert.Equal(RaceDigest(plain), RaceDigest(stepper.Finish()));
    }

    [Fact]
    public void A_live_order_is_reproduced_by_reconstruction_from_the_committed_log()
    {
        var session = SessionLoader.LoadFlagship();

        // Drive the upcoming round live, boxing a player driver early (as the pit-wall button does).
        var live = new LiveCareer(session);
        var round = live.UpcomingRound!.Round;
        var driver = live.Current.PlayerTeam()!.DriverIds[0];
        var stepper = live.StartLiveRace()!;
        var log = new List<RaceCommand>();
        while (!stepper.IsComplete)
        {
            if (stepper.CurrentLap == 2)
            {
                var command = new RaceCommand { Round = round, Lap = 3, DriverId = driver, Kind = RaceCommandKind.BoxThisLap };
                stepper.Issue(command);
                log.Add(command);
            }

            stepper.AdvanceLap();
        }

        var liveResult = stepper.Finish();

        // The committed log rides the season-start carset; a fresh (save/load) career reconstructs the round and
        // must reproduce the live race bit-for-bit — live == reconstruct.
        var committed = session with { Carset = session.Carset with { PlayerRaceCommands = log } };
        var reconstructed = AdvanceToFirstRace(new LiveCareer(committed)).Results[0];

        Assert.Equal(RaceDigest(liveResult), RaceDigest(reconstructed));
    }

    [Fact]
    public void Race_live_enters_live_mode_and_projects_the_stepper()
    {
        var live = new LiveCareer(SessionLoader.LoadFlagship());
        var vm = new RaceWeekendViewModel(live, startLive: live.StartLiveRace, commitLive: _ => { });

        Assert.True(vm.HasLiveOption);
        Assert.True(vm.ShowStrategy);
        Assert.False(vm.IsLive);

        vm.RaceLiveCommand.Execute(null);

        Assert.True(vm.IsLive);
        Assert.True(vm.IsPlaying);
        Assert.True(vm.TotalLaps > 1);
        Assert.False(vm.ShowStrategy);   // the strategy card gives way to the pit wall
        Assert.True(vm.ShowRaceArea);

        vm.AdvanceLap();
        Assert.Equal(1, vm.CurrentLap);
        Assert.NotEmpty(vm.Tower);       // live telemetry is projected exactly like the replay
        Assert.NotEmpty(vm.Markers);
    }

    [Fact]
    public void A_read_only_race_weekend_offers_no_live_option()
    {
        var vm = new RaceWeekendViewModel(new LiveCareer(SessionLoader.LoadFlagship())); // no hooks
        Assert.False(vm.HasLiveOption);
    }

    [Fact]
    public void Racing_live_to_the_flag_commits_the_order_through_the_shell()
    {
        var catalog = CarsetCatalog.Discover();
        var dir = Directory.CreateTempSubdirectory().FullName;
        var saves = new SaveStore(catalog, dir);
        var settings = new SettingsStore(Path.Combine(dir, "settings.json"));
        var root = new RootViewModel(new AppServices(catalog, saves, settings, new CareerNotificationSource()));

        root.EnterCareer(SessionLoader.LoadFlagship());
        var shell = (ShellViewModel)root.Content!;
        shell.Navigation.Navigate(NavKey.RaceWeekend);
        var vm = (RaceWeekendViewModel)shell.Navigation.CurrentScreen!;

        Assert.True(vm.HasLiveOption);
        vm.RaceLiveCommand.Execute(null);
        var expectedDriver = vm.LiveDrivers[0].DriverId;

        var guard = 0;
        while (vm.IsLive && guard++ < 5000)
        {
            if (vm.CurrentLap == 2)
            {
                vm.LiveDrivers[0].BoxCommand.Execute(null); // box this player driver next lap
            }

            vm.AdvanceLap();
        }

        Assert.False(vm.IsLive); // the race reached the flag and committed

        // The order was recorded on the season-start carset and autosaved, so a reload carries it.
        var reloaded = saves.Load(saves.MostRecent()!);
        Assert.NotEmpty(reloaded.Carset.PlayerRaceCommands);
        Assert.Equal(expectedDriver, reloaded.Carset.PlayerRaceCommands[0].DriverId);
        Assert.Equal(RaceCommandKind.BoxThisLap, reloaded.Carset.PlayerRaceCommands[0].Kind);
    }
}
