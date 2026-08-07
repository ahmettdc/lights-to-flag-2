using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Avalonia.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LTF.App.Mvvm;
using LTF.App.Session;
using LTF.Domain.Common;
using LTF.Simulation.Racing;

namespace LTF.App.ViewModels.Screens;

/// <summary>
/// The race-weekend screen (M23): a live timing tower that <em>replays</em> the deterministic race the career
/// round just ran. The engine already records the whole race lap-by-lap (<see cref="RaceTelemetry"/>), so the
/// tower is pure playback — no re-simulation, so the golden race is untouched. Steps through
/// <see cref="RaceTelemetry.Laps"/>: the running order (position, gap, interval, tyre, pit count), the flag
/// state, an event feed, and the final classification when the flag drops. A Continue rebuilds it, so it always
/// shows the most recent round.
/// </summary>
public sealed partial class RaceWeekendViewModel : ViewModelBase
{
    private readonly RaceResult? _result;
    private readonly IReadOnlyDictionary<string, string> _driverName;
    private readonly IReadOnlyDictionary<string, string> _teamName;
    private readonly IReadOnlyDictionary<string, string> _teamOfDriver;
    private readonly IReadOnlyDictionary<string, int> _teamIndex;

    public RaceWeekendViewModel(LiveCareer live)
    {
        var carset = live.Current;
        _driverName = carset.Drivers.ToDictionary(d => d.Id, d => d.FullName, StringComparer.Ordinal);
        _teamName = carset.Teams.ToDictionary(t => t.Id, t => t.Name, StringComparer.Ordinal);
        _teamOfDriver = carset.Teams
            .SelectMany(t => t.DriverIds.Select(id => (Driver: id, Team: t.Id)))
            .ToDictionary(x => x.Driver, x => x.Team, StringComparer.Ordinal);
        _teamIndex = carset.Teams
            .Select((t, i) => (t.Id, Index: i))
            .ToDictionary(x => x.Id, x => x.Index, StringComparer.Ordinal);

        var count = live.Results.Count;
        HasRace = count > 0;
        if (!HasRace)
        {
            HeaderText = "No race has run yet this season";
            Classification = [];
            return;
        }

        _result = live.Results[count - 1];
        var round = live.SeasonStart.Calendar[count - 1];
        var circuit = carset.Circuits.FirstOrDefault(c => string.CompareOrdinal(c.Id, round.CircuitId) == 0);
        HeaderText = string.Create(CultureInfo.InvariantCulture, $"Round {round.Round} · {circuit?.Name ?? round.CircuitId}");
        TotalLaps = _result.Telemetry.Laps.Count;

        Classification = _result.Classification
            .Select(e => new RaceResultRowViewModel(
                e.Position,
                Name(e.CompetitorId),
                TeamName(e.CompetitorId),
                Accent(e.CompetitorId),
                e.Status == FinishStatus.Finished
                    ? (e.Position <= 1 ? "WIN" : string.Create(CultureInfo.InvariantCulture, $"+{e.GapToLeader:0.000}"))
                    : Spaced(e.Status.ToString()),
                e.Points > 0 ? e.Points.ToString(CultureInfo.InvariantCulture) : ""))
            .ToList();

        SetLap(1);
    }

    /// <summary>True when a race has run and there is telemetry to replay; false shows the empty state.</summary>
    public bool HasRace { get; }

    public bool NoRace => !HasRace;

    public string HeaderText { get; } = "";

    public int TotalLaps { get; }

    /// <summary>The final classification (id → names joined); shown once the replay reaches the flag.</summary>
    public IReadOnlyList<RaceResultRowViewModel> Classification { get; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(LapText))]
    [NotifyPropertyChangedFor(nameof(RaceOver))]
    private int _currentLap;

    [ObservableProperty]
    private IReadOnlyList<TimingRowViewModel> _tower = [];

    [ObservableProperty]
    private IReadOnlyList<RaceEventRowViewModel> _feed = [];

    [ObservableProperty]
    private string _flagText = "";

    [ObservableProperty]
    private bool _isPlaying;

    /// <summary>Replay speed multiplier the view's timer reads (1×…6×).</summary>
    [ObservableProperty]
    private double _speed = 2.0;

    public string LapText => HasRace
        ? string.Create(CultureInfo.InvariantCulture, $"LAP {CurrentLap} / {TotalLaps}")
        : "";

    /// <summary>True once the replay has reached the chequered flag — the view reveals the classification.</summary>
    public bool RaceOver => HasRace && CurrentLap >= TotalLaps;

    /// <summary>Jump the replay to a lap and rebuild the tower + feed for it. Public so the view's timer and the
    /// tests can drive the replay directly.</summary>
    public void SetLap(int lap)
    {
        if (_result is null)
        {
            return;
        }

        CurrentLap = Math.Clamp(lap, 1, TotalLaps);
        var snapshot = _result.Telemetry.Laps[CurrentLap - 1];
        FlagText = FlagOf(snapshot.State);

        Tower = snapshot.Order
            .Select(s => new TimingRowViewModel(
                s.Position,
                Name(s.CompetitorId),
                TeamName(s.CompetitorId),
                Accent(s.CompetitorId),
                s.Position <= 1 ? "LEADER" : string.Create(CultureInfo.InvariantCulture, $"+{s.GapToLeader:0.0}"),
                s.Position <= 1 ? "—" : string.Create(CultureInfo.InvariantCulture, $"+{s.IntervalAhead:0.0}"),
                string.Create(CultureInfo.InvariantCulture, $"{Compound(s.TyreCompound)} {s.TyreAge}"),
                PitsUpTo(s.CompetitorId, CurrentLap)))
            .ToList();

        Feed = _result.Events
            .Where(e => e.Lap <= CurrentLap)
            .OrderByDescending(e => e.Lap)
            .Take(14)
            .Select(e => new RaceEventRowViewModel(
                e.Lap,
                string.Create(CultureInfo.InvariantCulture, $"L{e.Lap} · {e.Description}"),
                EventBrush(e.Kind)))
            .ToList();
    }

    /// <summary>Advance one lap; stops the playback at the flag. Called by the view's replay timer.</summary>
    public void AdvanceLap()
    {
        if (CurrentLap >= TotalLaps)
        {
            IsPlaying = false;
            return;
        }

        SetLap(CurrentLap + 1);
    }

    [RelayCommand]
    private void StepForward() => SetLap(CurrentLap + 1);

    [RelayCommand]
    private void StepBack() => SetLap(CurrentLap - 1);

    [RelayCommand]
    private void Restart()
    {
        IsPlaying = false;
        SetLap(1);
    }

    [RelayCommand]
    private void SkipToEnd()
    {
        IsPlaying = false;
        SetLap(TotalLaps);
    }

    [RelayCommand]
    private void TogglePlay() => IsPlaying = !IsPlaying;

    private string Name(string id) => _driverName.GetValueOrDefault(id, id);

    private string TeamName(string id) =>
        _teamName.GetValueOrDefault(_teamOfDriver.GetValueOrDefault(id, ""), "");

    private IBrush Accent(string id) =>
        ScreenBrushes.TeamAccent(_teamIndex.GetValueOrDefault(_teamOfDriver.GetValueOrDefault(id, ""), 0));

    private int PitsUpTo(string competitorId, int lap)
    {
        if (_result is null)
        {
            return 0;
        }

        var count = 0;
        foreach (var e in _result.Events)
        {
            if (e.Kind == RaceEventKind.Pit && e.Lap <= lap
                && string.CompareOrdinal(e.CompetitorId, competitorId) == 0)
            {
                count++;
            }
        }

        return count;
    }

    private static string Compound(TyreCompound compound) => compound switch
    {
        TyreCompound.Soft => "S",
        TyreCompound.Medium => "M",
        TyreCompound.Hard => "H",
        TyreCompound.Intermediate => "I",
        TyreCompound.Wet => "W",
        _ => "?",
    };

    private static string FlagOf(NeutralizationState state) => state switch
    {
        NeutralizationState.SafetyCar => "SAFETY CAR",
        NeutralizationState.VirtualSafetyCar => "VSC",
        NeutralizationState.RedFlag => "RED FLAG",
        _ => "GREEN",
    };

    private static IBrush EventBrush(RaceEventKind kind) => kind switch
    {
        RaceEventKind.Overtake => ScreenBrushes.Good,
        RaceEventKind.Pit => ScreenBrushes.Warn,
        RaceEventKind.SafetyCar or RaceEventKind.VirtualSafetyCar or RaceEventKind.RedFlag => ScreenBrushes.Warn,
        RaceEventKind.MechanicalFailure or RaceEventKind.DriverError or RaceEventKind.Collision
            or RaceEventKind.StartIncident or RaceEventKind.Penalty => ScreenBrushes.Bad,
        _ => ScreenBrushes.Dim,
    };

    // Split a PascalCase enum name into words (DidNotStart → "Did Not Start").
    private static string Spaced(string pascal)
    {
        var sb = new System.Text.StringBuilder(pascal.Length + 4);
        for (var i = 0; i < pascal.Length; i++)
        {
            if (i > 0 && char.IsUpper(pascal[i]))
            {
                sb.Append(' ');
            }

            sb.Append(pascal[i]);
        }

        return sb.ToString();
    }
}

/// <summary>One row of the live timing tower (M23): order, names, gaps, tyre and pit count.</summary>
public sealed record TimingRowViewModel(
    int Position, string Driver, string Team, IBrush Accent, string Gap, string Interval, string Tyre, int PitStops);

/// <summary>One entry in the race event feed (M23).</summary>
public sealed record RaceEventRowViewModel(int Lap, string Text, IBrush Brush);

/// <summary>One line of the final classification on the race-weekend screen (M23).</summary>
public sealed record RaceResultRowViewModel(int Position, string Driver, string Team, IBrush Accent, string Status, string Points);
