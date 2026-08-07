using System;
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
using LTF.Domain;
using Xunit;

namespace LTF.App.Tests;

/// <summary>
/// The first in-shell career screens (M21a): Standings and Calendar project the read-only carset/clock,
/// resolve to their views via the app DataTemplates headless on all three platforms, and register through
/// the shell so navigating to them yields the real view-models (not the placeholder).
/// </summary>
public class Phase3ScreensTests
{
    private static Carset Flagship() => SessionLoader.LoadFlagship().Carset;

    // --- Standings ---

    [Fact]
    public void Standings_empty_table_lists_every_team_and_driver_on_zero()
    {
        var carset = Flagship();
        var vm = new StandingsViewModel(carset, ChampionshipStandings.Empty(carset));

        Assert.Equal(carset.Teams.Count, vm.Constructors.Count);
        Assert.Equal(carset.Drivers.Count, vm.Drivers.Count);
        Assert.Equal(carset.Drivers.Count, vm.DriverCount);

        Assert.All(vm.Constructors, c => Assert.Equal(0, c.Points));
        Assert.All(vm.Constructors, c => Assert.Equal(0d, c.BarWidth));
        Assert.All(vm.Drivers, d => Assert.Equal(0, d.Points));

        // Positions run 1..N and names are resolved off the carset (never a raw id or blank).
        Assert.Equal(Enumerable.Range(1, carset.Teams.Count), vm.Constructors.Select(c => c.Position));
        Assert.All(vm.Constructors, c => Assert.False(string.IsNullOrWhiteSpace(c.Team)));
        Assert.All(vm.Drivers, d => Assert.False(string.IsNullOrWhiteSpace(d.Name)));
    }

    [Fact]
    public void Standings_points_bar_is_leader_relative()
    {
        var carset = Flagship();
        var standings = new Standings
        {
            Constructors = new[]
            {
                new ConstructorStanding { Position = 1, TeamId = carset.Teams[0].Id, Points = 50, Wins = 2 },
                new ConstructorStanding { Position = 2, TeamId = carset.Teams[1].Id, Points = 25, Wins = 1 },
            },
            Drivers = Array.Empty<DriverStanding>(),
        };

        var vm = new StandingsViewModel(carset, standings);

        Assert.Equal(96d, vm.Constructors[0].BarWidth); // leader fills the 96px track
        Assert.Equal(48d, vm.Constructors[1].BarWidth); // half the points → half the bar
    }

    [AvaloniaFact]
    public void Standings_view_resolves_from_its_view_model()
    {
        var carset = Flagship();
        var host = new ContentControl { Content = new StandingsViewModel(carset, ChampionshipStandings.Empty(carset)) };
        var window = new Window { Content = host };

        window.Show();
        Dispatcher.UIThread.RunJobs();

        Assert.Single(host.GetVisualDescendants().OfType<StandingsView>());
    }

    // --- Calendar ---

    [Fact]
    public void Calendar_marks_rounds_relative_to_the_clock()
    {
        var carset = Flagship();
        var rounds = carset.Calendar.OrderBy(r => r.Round).ToList();

        // At the season's open (the day before round 1) the first round is next and none are done.
        var atStart = new CalendarViewModel(carset, CareerClock.Start(carset));
        Assert.Equal(rounds.Count, atStart.Rounds.Count);
        Assert.Equal(RoundStatus.Next, atStart.Rounds[0].Status);
        Assert.DoesNotContain(atStart.Rounds, r => r.Status == RoundStatus.Done);
        Assert.True(atStart.HasNextRace);

        // A day after round 1 it is done, and the following round becomes next.
        var afterR1 = new CalendarViewModel(carset, CareerClock.Start(carset) with { Date = rounds[0].Date.AddDays(1) });
        Assert.Equal(RoundStatus.Done, afterR1.Rounds[0].Status);
        Assert.Equal(RoundStatus.Next, afterR1.Rounds[1].Status);
    }

    [Fact]
    public void Calendar_next_race_profile_reflects_the_next_circuit()
    {
        var carset = Flagship();
        var vm = new CalendarViewModel(carset, CareerClock.Start(carset));

        Assert.True(vm.HasNextRace);
        Assert.StartsWith("NEXT RACE", vm.NextRaceHeading);
        Assert.NotEmpty(vm.Profile);
        Assert.Contains(vm.Profile, p => p.Label == "Length");
    }

    [Fact]
    public void Calendar_past_the_final_round_reports_the_season_complete()
    {
        var carset = Flagship();
        var lastDate = carset.Calendar.Max(r => r.Date);
        var vm = new CalendarViewModel(carset, CareerClock.Start(carset) with { Date = lastDate.AddDays(1) });

        Assert.False(vm.HasNextRace);
        Assert.Equal("SEASON COMPLETE", vm.NextRaceHeading);
        Assert.All(vm.Rounds, r => Assert.Equal(RoundStatus.Done, r.Status));
    }

    [AvaloniaFact]
    public void Calendar_view_resolves_from_its_view_model()
    {
        var carset = Flagship();
        var host = new ContentControl { Content = new CalendarViewModel(carset, CareerClock.Start(carset)) };
        var window = new Window { Content = host };

        window.Show();
        Dispatcher.UIThread.RunJobs();

        Assert.Single(host.GetVisualDescendants().OfType<CalendarView>());
    }

    // --- Registration through the shell ---

    [Fact]
    public void Entering_a_career_registers_the_standings_and_calendar_screens()
    {
        var catalog = CarsetCatalog.Discover();
        var dir = Directory.CreateTempSubdirectory().FullName;
        var saves = new SaveStore(catalog, dir);
        var settings = new SettingsStore(Path.Combine(dir, "settings.json"));
        var root = new RootViewModel(new AppServices(catalog, saves, settings, new SampleNotificationSource()));

        root.EnterCareer(SessionLoader.LoadFlagship());
        var shell = (ShellViewModel)root.Content!;

        shell.Navigation.Navigate(NavKey.Standings);
        Assert.IsType<StandingsViewModel>(shell.Navigation.CurrentScreen);

        shell.Navigation.Navigate(NavKey.Calendar);
        Assert.IsType<CalendarViewModel>(shell.Navigation.CurrentScreen);
    }
}
