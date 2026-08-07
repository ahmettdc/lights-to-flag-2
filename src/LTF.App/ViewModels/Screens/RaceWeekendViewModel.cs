using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Avalonia.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LTF.App.Mvvm;
using LTF.App.Session;
using LTF.Domain;
using LTF.Domain.Common;
using LTF.Simulation.Qualifying;
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
    private readonly IReadOnlySet<string> _playerDrivers;
    private double _referenceLap = 90.0;

    public RaceWeekendViewModel(
        LiveCareer live,
        Action<int, string, TyreCompound>? setStrategy = null,
        Action? startRace = null)
    {
        _startRace = startRace;

        var carset = live.Current;
        _driverName = carset.Drivers.ToDictionary(d => d.Id, d => d.FullName, StringComparer.Ordinal);
        _teamName = carset.Teams.ToDictionary(t => t.Id, t => t.Name, StringComparer.Ordinal);
        _teamOfDriver = carset.Teams
            .SelectMany(t => t.DriverIds.Select(id => (Driver: id, Team: t.Id)))
            .ToDictionary(x => x.Driver, x => x.Team, StringComparer.Ordinal);
        _teamIndex = carset.Teams
            .Select((t, i) => (t.Id, Index: i))
            .ToDictionary(x => x.Id, x => x.Index, StringComparer.Ordinal);
        _playerDrivers = new HashSet<string>(carset.PlayerTeam()?.DriverIds ?? [], StringComparer.Ordinal);

        var count = live.Results.Count;

        // Strategy panel (M23b): the upcoming (not-yet-run) round's player-team drivers and each one's chosen
        // starting compound. Built independently of the replay, so a brand-new career (no race yet) still
        // shows it and a season finale (no upcoming race) simply has none.
        Strategy = BuildStrategy(carset, count, setStrategy, out var upcomingText);
        UpcomingText = upcomingText;
        HasUpcoming = Strategy.Count > 0;

        HasRace = count > 0;
        if (!HasRace)
        {
            HeaderText = "No race has run yet this season";
            Classification = [];
            QualifyingGrid = [];
            return;
        }

        _result = live.Results[count - 1];
        var round = live.SeasonStart.Calendar[count - 1];
        var circuit = carset.Circuits.FirstOrDefault(c => string.CompareOrdinal(c.Id, round.CircuitId) == 0);
        var refLap = circuit?.BaseLapTimeSeconds ?? 0.0;
        _referenceLap = refLap > 0 ? refLap : 90.0; // converts time gaps to a fraction of a lap for the map
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

        // Qualifying grid for this round (M23d): reconstructed alongside the results, parallel to live.Results.
        QualifyingGrid = BuildQualifying(live.Qualifying[count - 1]);

        SetLap(1);
    }

    /// <summary>True when a race has run and there is telemetry to replay; false shows the empty state.</summary>
    public bool HasRace { get; }

    public bool NoRace => !HasRace;

    public string HeaderText { get; } = "";

    public int TotalLaps { get; }

    /// <summary>The final classification (id → names joined); shown once the replay reaches the flag.</summary>
    public IReadOnlyList<RaceResultRowViewModel> Classification { get; }

    /// <summary>The qualifying grid for this round (M23d): position, driver, the part their time was set in,
    /// their grid-deciding lap and its sector split. Empty until a race has run.</summary>
    public IReadOnlyList<QualifyingRowViewModel> QualifyingGrid { get; } = [];

    // The host's "advance the career" step (M23b), invoked by Start Race; null on a read-only screen.
    private readonly Action? _startRace;

    /// <summary>True when there is an upcoming (not-yet-run) round to set a starting strategy for.</summary>
    public bool HasUpcoming { get; }

    /// <summary>The upcoming round's label (round number · circuit), for the strategy panel header.</summary>
    public string UpcomingText { get; } = "";

    /// <summary>One row per player-team driver for the upcoming round: their chosen starting compound (M23b).</summary>
    public IReadOnlyList<StrategyRowViewModel> Strategy { get; }

    /// <summary>Whether Start Race can run: the screen can advance the career and a race is upcoming.</summary>
    public bool CanStartRace => _startRace is not null && HasUpcoming;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(LapText))]
    [NotifyPropertyChangedFor(nameof(RaceOver))]
    private int _currentLap;

    [ObservableProperty]
    private IReadOnlyList<TimingRowViewModel> _tower = [];

    /// <summary>The car markers on the schematic track map at the current lap (M23e): position approximated
    /// from each car's time gap to the leader, laid along the outline. Rebuilt each <see cref="SetLap"/>.</summary>
    [ObservableProperty]
    private IReadOnlyList<TrackMarkerViewModel> _markers = [];

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

        // Track-map markers (M23e): a car g laps' worth of time behind the leader sits g of a lap short of the
        // start/finish line. Pure projection of the recorded gaps — no engine data, so the golden race is safe.
        Markers = snapshot.Order
            .Select(s =>
            {
                var behind = _referenceLap > 0 ? s.GapToLeader / _referenceLap : 0.0;
                var point = TrackOutline.PointAtFraction(-behind);
                var player = _playerDrivers.Contains(s.CompetitorId);
                var size = player ? 15.0 : 11.0;
                return new TrackMarkerViewModel(
                    point.X - (size / 2), point.Y - (size / 2), size,
                    Accent(s.CompetitorId), player ? Brushes.White : Brushes.Transparent, Name(s.CompetitorId));
            })
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

    /// <summary>Advance the career to run the upcoming race (M23b) with the chosen starting tyres; the shell
    /// re-navigates afterward and the tower replays the just-run race.</summary>
    [RelayCommand(CanExecute = nameof(CanStartRace))]
    private void StartRace() => _startRace?.Invoke();

    // Build the strategy rows for the upcoming (not-yet-run) round (M23b): the player team's drivers and each
    // one's chosen starting compound (defaulting to Medium). Empty — the panel hides — when there is no player
    // team or no upcoming round (season finale). Sets upcomingText to the round's label for the panel header.
    private IReadOnlyList<StrategyRowViewModel> BuildStrategy(
        Carset carset, int racesRun, Action<int, string, TyreCompound>? setStrategy, out string upcomingText)
    {
        upcomingText = "";
        var team = carset.PlayerTeam();
        if (team is null || racesRun >= carset.Calendar.Count)
        {
            return [];
        }

        var upcoming = carset.Calendar[racesRun];
        var circuit = carset.Circuits.FirstOrDefault(c => string.CompareOrdinal(c.Id, upcoming.CircuitId) == 0);
        upcomingText = string.Create(CultureInfo.InvariantCulture, $"Round {upcoming.Round} · {circuit?.Name ?? upcoming.CircuitId}");

        var chosen = carset.PlayerRaceStrategies
            .Where(s => s.Round == upcoming.Round)
            .ToDictionary(s => s.DriverId, s => s.Compound, StringComparer.Ordinal);

        return team.DriverIds
            .Select(id => new StrategyRowViewModel(
                upcoming.Round, id, Name(id), chosen.GetValueOrDefault(id, TyreCompound.Medium), setStrategy))
            .ToList();
    }

    private IReadOnlyList<QualifyingRowViewModel> BuildQualifying(QualifyingResult quali) =>
        quali.Grid
            .Select(e => new QualifyingRowViewModel(
                e.GridPosition,
                Name(e.CompetitorId),
                TeamName(e.CompetitorId),
                Accent(e.CompetitorId),
                string.Create(CultureInfo.InvariantCulture, $"Q{e.Part}"),
                LapTime(e.BestLap),
                e.BestSectors.Total > 0
                    ? string.Create(CultureInfo.InvariantCulture,
                        $"{e.BestSectors.Sector1:0.000}   {e.BestSectors.Sector2:0.000}   {e.BestSectors.Sector3:0.000}")
                    : "—"))
            .ToList();

    // Format a lap time in seconds as M:SS.mmm (e.g. 80.5 → "1:20.500"); an empty/zero time shows a dash.
    private static string LapTime(double seconds)
    {
        if (seconds <= 0)
        {
            return "—";
        }

        var minutes = (int)(seconds / 60);
        var rest = seconds - (minutes * 60);
        return string.Create(CultureInfo.InvariantCulture, $"{minutes}:{rest:00.000}");
    }

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

/// <summary>One row of the qualifying-grid tab on the race-weekend screen (M23d).</summary>
public sealed record QualifyingRowViewModel(
    int GridPosition, string Driver, string Team, IBrush Accent, string Part, string BestLap, string Sectors);

/// <summary>One car's marker on the schematic track map (M23e): the top-left canvas position of a dot of the
/// given <paramref name="Size"/>, filled with the team colour, with a highlight <paramref name="Stroke"/> on
/// the player's cars, and the driver name for a tooltip.</summary>
public sealed record TrackMarkerViewModel(
    double X, double Y, double Size, IBrush Fill, IBrush Stroke, string Name);

/// <summary>
/// One driver's pre-race strategy row (M23b): the starting compound the player picks for the upcoming round.
/// Changing <see cref="SelectedCompound"/> calls back to the host, which lands the choice on the season-start
/// carset and re-navigates — so the picker is a mutation trigger, not local state. The initial assignment is
/// suppressed so building the row from a saved choice does not fire the callback.
/// </summary>
public sealed partial class StrategyRowViewModel : ObservableObject
{
    private readonly int _round;
    private readonly string _driverId;
    private readonly Action<int, string, TyreCompound>? _setStrategy;
    private bool _suppress;

    public StrategyRowViewModel(
        int round, string driverId, string driverName, TyreCompound selected,
        Action<int, string, TyreCompound>? setStrategy)
    {
        _round = round;
        _driverId = driverId;
        DriverName = driverName;
        _setStrategy = setStrategy;

        _suppress = true;
        SelectedCompound = selected;
        _suppress = false;
    }

    public string DriverName { get; }

    /// <summary>The compounds a player may start on — slick only; wets are weather-driven, not a pre-race pick.</summary>
    public IReadOnlyList<TyreCompound> Compounds { get; } =
        [TyreCompound.Soft, TyreCompound.Medium, TyreCompound.Hard];

    /// <summary>False on a read-only screen (no callback), which disables the picker.</summary>
    public bool CanEdit => _setStrategy is not null;

    [ObservableProperty]
    private TyreCompound _selectedCompound;

    partial void OnSelectedCompoundChanged(TyreCompound value)
    {
        if (_suppress)
        {
            return;
        }

        _setStrategy?.Invoke(_round, _driverId, value);
    }
}
