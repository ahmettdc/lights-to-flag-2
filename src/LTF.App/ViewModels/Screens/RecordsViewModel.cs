using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Avalonia.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using LTF.App.Mvvm;
using LTF.App.Session;
using LTF.Domain.Racing;

namespace LTF.App.ViewModels.Screens;

/// <summary>
/// The records &amp; statistics screen (M24): career profiles, this-season statistics, all-time record boards and
/// the hall of fame, over a career's accumulated history. A read-only projection over the live career — the driver
/// and team career tallies (accumulated at each season rollover) and the persisted season archive — so a Continue
/// rebuilds it. No engine data is touched, so the golden race is untouched.
/// </summary>
public sealed partial class RecordsViewModel : ViewModelBase
{
    public RecordsViewModel(LiveCareer live)
    {
        var carset = live.Current;

        var driverName = carset.Drivers.ToDictionary(d => d.Id, d => d.FullName, StringComparer.Ordinal);
        var circuitName = carset.Circuits.ToDictionary(c => c.Id, c => c.Name, StringComparer.Ordinal);
        var teamName = new Dictionary<string, string>(StringComparer.Ordinal);
        var teamOfDriver = new Dictionary<string, string>(StringComparer.Ordinal);
        var teamAccent = new Dictionary<string, IBrush>(StringComparer.Ordinal);
        for (var i = 0; i < carset.Teams.Count; i++)
        {
            var team = carset.Teams[i];
            teamName[team.Id] = team.Name;
            teamAccent[team.Id] = ScreenBrushes.TeamAccent(i);
            foreach (var id in team.DriverIds)
            {
                teamOfDriver[id] = team.Id;
            }
        }

        IBrush AccentOf(string driverId) =>
            teamOfDriver.TryGetValue(driverId, out var teamId)
                ? teamAccent.GetValueOrDefault(teamId, ScreenBrushes.Faint)
                : ScreenBrushes.Faint;

        // Career profiles (PROFILES): every driver's accumulated record, ranked by career points then wins.
        Profiles = carset.Drivers
            .OrderByDescending(d => d.Career.Points)
            .ThenByDescending(d => d.Career.Wins)
            .ThenBy(d => d.FullName, StringComparer.Ordinal)
            .Select((d, i) =>
            {
                var teamId = teamOfDriver.GetValueOrDefault(d.Id, "");
                var teamLabel = teamId.Length > 0 ? teamName.GetValueOrDefault(teamId, "") : "Free agent";
                return new CareerProfileRowViewModel(i + 1, d, teamLabel, AccentOf(d.Id));
            })
            .ToList();
        _selectedProfile = Profiles.FirstOrDefault();

        // All-time record boards (ALL-TIME): the top of each cumulative tally, drivers then teams.
        RecordBoardViewModel DriverBoard(string title, Func<DriverCareer, double> stat) => new(
            title,
            carset.Drivers
                .OrderByDescending(d => stat(d.Career))
                .ThenBy(d => d.FullName, StringComparer.Ordinal)
                .Take(5)
                .Select((d, i) => new RecordEntryViewModel(
                    Ordinal(i), d.FullName, AccentOf(d.Id), stat(d.Career).ToString("0", CultureInfo.InvariantCulture)))
                .ToList());

        RecordBoardViewModel TeamBoard(string title, Func<Team, double> stat) => new(
            title,
            carset.Teams
                .Select((t, i) => (Team: t, Accent: ScreenBrushes.TeamAccent(i)))
                .OrderByDescending(x => stat(x.Team))
                .ThenBy(x => x.Team.Name, StringComparer.Ordinal)
                .Take(5)
                .Select((x, i) => new RecordEntryViewModel(
                    Ordinal(i), x.Team.Name, x.Accent, stat(x.Team).ToString("0", CultureInfo.InvariantCulture)))
                .ToList());

        AllTimeBoards = new[]
        {
            DriverBoard("MOST CHAMPIONSHIPS", c => c.Championships),
            DriverBoard("MOST WINS", c => c.Wins),
            DriverBoard("MOST POLES", c => c.Poles),
            DriverBoard("MOST PODIUMS", c => c.Podiums),
            DriverBoard("MOST POINTS", c => c.Points),
            DriverBoard("MOST FASTEST LAPS", c => c.FastestLaps),
            TeamBoard("CONSTRUCTORS' TITLES", t => t.ChampionshipsWon),
            TeamBoard("TEAM RACE WINS", t => t.RaceWins),
        };

        // Hall of fame (HALL OF FAME): the season champions roll, most recent first, plus the track records.
        HallOfFame = carset.SeasonHistory
            .Reverse()
            .Select(s => new SeasonChampionViewModel(
                s.Year.ToString(CultureInfo.InvariantCulture),
                driverName.GetValueOrDefault(s.DriversChampionId, s.DriversChampionId),
                AccentOf(s.DriversChampionId),
                teamName.GetValueOrDefault(s.ConstructorsChampionId, s.ConstructorsChampionId)))
            .ToList();

        TrackRecords = carset.TrackRecords
            .Select(t => new TrackRecordViewModel(
                circuitName.GetValueOrDefault(t.CircuitId, t.CircuitId),
                driverName.GetValueOrDefault(t.DriverId, t.DriverId),
                FormatLap(t.BestLapSeconds),
                t.Year.ToString(CultureInfo.InvariantCulture)))
            .ToList();

        HasHistory = carset.SeasonHistory.Count > 0;
    }

    /// <summary>Every driver's career record, ranked (PROFILES tab).</summary>
    public IReadOnlyList<CareerProfileRowViewModel> Profiles { get; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ProfileDetail))]
    private CareerProfileRowViewModel? _selectedProfile;

    /// <summary>The full profile card for the selected driver (the shared driver/team detail panel).</summary>
    public EntityDetailViewModel? ProfileDetail => SelectedProfile?.Detail;

    /// <summary>The all-time record leaderboards (ALL-TIME tab): the top of each cumulative tally.</summary>
    public IReadOnlyList<RecordBoardViewModel> AllTimeBoards { get; }

    /// <summary>The season champions roll, most recent first (HALL OF FAME tab).</summary>
    public IReadOnlyList<SeasonChampionViewModel> HallOfFame { get; }

    /// <summary>The fastest race lap ever set at each circuit (HALL OF FAME tab).</summary>
    public IReadOnlyList<TrackRecordViewModel> TrackRecords { get; }

    /// <summary>True once at least one season has completed — the hall of fame has content.</summary>
    public bool HasHistory { get; }

    // "1"…"5" for a leaderboard row (the display rank).
    private static string Ordinal(int index) => (index + 1).ToString(CultureInfo.InvariantCulture);

    // A lap time in seconds as M:SS.mmm (e.g. 80.5 → "1:20.500"); an empty/zero time shows a dash.
    private static string FormatLap(double seconds)
    {
        if (seconds <= 0)
        {
            return "—";
        }

        var minutes = (int)(seconds / 60);
        var rest = seconds - (minutes * 60);
        return string.Create(CultureInfo.InvariantCulture, $"{minutes}:{rest:00.000}");
    }
}

/// <summary>One all-time record leaderboard (M24): a titled top-five of a cumulative tally.</summary>
public sealed record RecordBoardViewModel(string Title, IReadOnlyList<RecordEntryViewModel> Entries);

/// <summary>One row on a record leaderboard (M24): rank, name, team accent and the tally value.</summary>
public sealed record RecordEntryViewModel(string Rank, string Name, IBrush Accent, string Value);

/// <summary>One year's champions in the hall of fame (M24): the season year and its two champions.</summary>
public sealed record SeasonChampionViewModel(string Year, string DriverChampion, IBrush Accent, string ConstructorChampion);

/// <summary>One circuit's lap record (M24): circuit, holder, the lap time and the year it was set.</summary>
public sealed record TrackRecordViewModel(string Circuit, string Driver, string LapTime, string Year);

/// <summary>One driver's row in the career-profiles table (M24): rank, name, team accent, the headline career
/// tallies, and the profile card the detail panel shows when the row is selected.</summary>
public sealed class CareerProfileRowViewModel
{
    public CareerProfileRowViewModel(int rank, Driver driver, string teamLabel, IBrush accent)
    {
        Rank = rank.ToString(CultureInfo.InvariantCulture);
        Name = driver.FullName;
        Accent = accent;
        Races = driver.Career.Races.ToString(CultureInfo.InvariantCulture);
        Wins = driver.Career.Wins.ToString(CultureInfo.InvariantCulture);
        Podiums = driver.Career.Podiums.ToString(CultureInfo.InvariantCulture);
        Poles = driver.Career.Poles.ToString(CultureInfo.InvariantCulture);
        Titles = driver.Career.Championships.ToString(CultureInfo.InvariantCulture);
        Points = driver.Career.Points.ToString("0", CultureInfo.InvariantCulture);
        Detail = EntityDetailViewModel.ForDriver(driver, teamLabel);
    }

    public string Rank { get; }
    public string Name { get; }
    public IBrush Accent { get; }
    public string Races { get; }
    public string Wins { get; }
    public string Podiums { get; }
    public string Poles { get; }
    public string Titles { get; }
    public string Points { get; }
    public EntityDetailViewModel Detail { get; }
}
