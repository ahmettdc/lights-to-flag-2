using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LightsToFlag.App.Services;
using LightsToFlag.Core.Career;
using LightsToFlag.Core.Domain;
using LightsToFlag.Core.Simulation;

namespace LightsToFlag.App.ViewModels;

public sealed record LiveRow(int Position, string Name, string Gap, TyreCompound Tyre, bool InPit, string Status, bool IsPlayer);
public sealed record ResultRow(int Position, string Name, string Team, string Status, double Points, bool FastestLap, bool IsPlayer);

/// <summary>
/// Runs the player's race weekend interactively: practice → qualifying → race with
/// live timing playback → results. Uses the same deterministic per-round seed the
/// career engine would, then commits the round to the career.
/// </summary>
public partial class RaceWeekendViewModel : ObservableObject
{
    private readonly GameSession _session;
    private readonly INavigationService _nav;
    private readonly Carset _carset;
    private readonly Coefficients _coeff;
    private readonly RulesSet _rules;
    private readonly CircuitSpec _circuit;
    private readonly int _roundIndex;
    private readonly IRandom _rng;
    private readonly Dictionary<string, string> _names;
    private readonly string? _playerId;

    private IReadOnlyList<Competitor> _competitors;
    private QualifyingResult? _grid;
    private RaceTelemetry? _telemetry;
    private DispatcherTimer? _timer;
    private int _lapIndex;

    public RaceWeekendViewModel(GameSession session, INavigationService nav)
    {
        _session = session;
        _nav = nav;
        _carset = session.Carset!;
        var career = session.Career!;
        _coeff = _carset.Coefficients;
        _rules = _carset.Rules;
        _playerId = career.PlayerId;

        _roundIndex = session.Engine.NextRoundIndex(career);
        _circuit = session.Engine.NextCircuit(career, _carset);
        _rng = new SeededRandom(session.Engine.SeedForRound(career, _roundIndex));
        _competitors = session.Engine.BuildEntryList(career, _carset);
        _names = career.Entrants.ToDictionary(e => e.Id, e => e.Driver.FullName);

        CircuitName = _circuit.Name;
        RoundLabel = $"Round {_roundIndex + 1} · {_circuit.Venue}";
        Stage = "practice";
    }

    public ObservableCollection<LiveRow> Live { get; } = new();
    public ObservableCollection<ResultRow> Results { get; } = new();
    public IReadOnlyList<TyreCompound> TyreChoices { get; } = new[] { TyreCompound.Soft, TyreCompound.Hard };

    [ObservableProperty] private string _stage = "practice";
    [ObservableProperty] private string _circuitName = "";
    [ObservableProperty] private string _roundLabel = "";
    [ObservableProperty] private int _practiceLaps = 20;
    [ObservableProperty] private string _practiceResult = "";
    [ObservableProperty] private bool _qualifyingDone;
    [ObservableProperty] private string _qualifyingResult = "";
    [ObservableProperty] private TyreCompound _startingTyre = TyreCompound.Soft;
    [ObservableProperty] private int _currentLap;
    [ObservableProperty] private int _totalLaps;
    [ObservableProperty] private bool _isPlaying;
    [ObservableProperty] private double _speed = 1.0;
    [ObservableProperty] private string _winnerText = "";

    [RelayCommand]
    private void RunPractice()
    {
        var laps = _playerId is null
            ? null
            : new Dictionary<string, int> { [_playerId] = PracticeLaps };
        _competitors = PracticeSimulator.Run(_competitors, _coeff, _rng, laps);

        if (_playerId is not null)
        {
            var setup = _competitors.First(c => c.Id == _playerId).SetupQuality;
            PracticeResult = $"Setup dialled in to {setup * 100:0}%.";
        }
        else
        {
            PracticeResult = "Practice complete.";
        }

        Stage = "qualifying";
    }

    [RelayCommand]
    private void RunQualifying()
    {
        _grid = QualifyingSimulator.Run(_competitors, _circuit, _coeff, _rules, _rng);
        var pole = _names.GetValueOrDefault(_grid.PolePosition, _grid.PolePosition);
        var playerPos = _playerId is not null ? _grid.PositionOf(_playerId) : 0;
        QualifyingResult = playerPos > 0
            ? $"Pole: {pole}.  You qualified P{playerPos}."
            : $"Pole: {pole}.";
        QualifyingDone = true;
    }

    [RelayCommand]
    private void StartRace()
    {
        if (_grid is null)
        {
            return;
        }

        var startingTyres = _playerId is not null
            ? new Dictionary<string, TyreCompound> { [_playerId] = StartingTyre }
            : null;

        _telemetry = new RaceSimulator()
            .RunWithTelemetry(_competitors, _grid, _circuit, _coeff, _rules, _rng, startingTyres);

        TotalLaps = _telemetry.Final.TotalLaps;
        _lapIndex = 0;
        Stage = "race";
        RenderLap(0);
        StartTimer();
        IsPlaying = true;
    }

    private void StartTimer()
    {
        _timer ??= new DispatcherTimer();
        _timer.Tick -= OnTick;
        _timer.Tick += OnTick;
        _timer.Interval = System.TimeSpan.FromMilliseconds(600 / System.Math.Max(0.25, Speed));
        _timer.Start();
    }

    partial void OnSpeedChanged(double value)
    {
        if (_timer is not null && IsPlaying)
        {
            _timer.Interval = System.TimeSpan.FromMilliseconds(600 / System.Math.Max(0.25, value));
        }
    }

    private void OnTick(object? sender, System.EventArgs e)
    {
        if (_telemetry is null)
        {
            return;
        }

        _lapIndex++;
        if (_lapIndex >= _telemetry.Laps.Count)
        {
            FinishRace();
            return;
        }

        RenderLap(_lapIndex);
    }

    private void RenderLap(int index)
    {
        if (_telemetry is null || _telemetry.Laps.Count == 0)
        {
            return;
        }

        var snap = _telemetry.Laps[System.Math.Clamp(index, 0, _telemetry.Laps.Count - 1)];
        CurrentLap = snap.Lap;

        Live.Clear();
        foreach (var row in snap.Order)
        {
            var name = _names.GetValueOrDefault(row.CompetitorId, row.CompetitorId);
            var gap = row.Status == FinishStatus.Retired
                ? "DNF"
                : row.Position == 1 ? "Leader" : $"+{row.GapToLeaderSeconds:0.0}s";
            var status = row.InPit ? "PIT" : row.Status == FinishStatus.Retired ? "OUT" : "";
            Live.Add(new LiveRow(row.Position, name, gap, row.Tyre, row.InPit, status, row.CompetitorId == _playerId));
        }
    }

    [RelayCommand]
    private void TogglePlay()
    {
        if (_timer is null)
        {
            return;
        }

        if (IsPlaying)
        {
            _timer.Stop();
            IsPlaying = false;
        }
        else
        {
            StartTimer();
            IsPlaying = true;
        }
    }

    [RelayCommand]
    private void SkipToEnd() => FinishRace();

    private void FinishRace()
    {
        _timer?.Stop();
        IsPlaying = false;
        if (_telemetry is null || _grid is null)
        {
            return;
        }

        if (_telemetry.Laps.Count > 0)
        {
            RenderLap(_telemetry.Laps.Count - 1);
        }

        CommitResult();
        BuildResults();
        Stage = "results";
    }

    private void CommitResult()
    {
        if (_session.Career is not { } career || _telemetry is null || _grid is null)
        {
            return;
        }

        var result = CareerEngine.BuildRoundResult(_roundIndex, _circuit.Name, _grid, _telemetry.Final);
        _session.Career = _session.Engine.RecordRound(career, result);
    }

    private void BuildResults()
    {
        if (_telemetry is null)
        {
            return;
        }

        var teamByEntrant = _session.Career!.Entrants.ToDictionary(en => en.Id, en => en.TeamNumber);
        Results.Clear();
        foreach (var e in _telemetry.Final.Entries)
        {
            var name = _names.GetValueOrDefault(e.CompetitorId, e.CompetitorId);
            var teamNo = teamByEntrant.GetValueOrDefault(e.CompetitorId, 0);
            var team = teamNo >= 1 && teamNo <= _carset.Teams.Count ? _carset.Teams[teamNo - 1].Name : "";
            var status = e.Status == FinishStatus.Retired ? (e.RetirementReason ?? "DNF") : "Finished";
            Results.Add(new ResultRow(e.Position, name, team, status, e.Points, e.FastestLap, e.CompetitorId == _playerId));
        }

        WinnerText = _telemetry.Final.Entries.Count > 0
            ? $"Winner: {_names.GetValueOrDefault(_telemetry.Final.Winner, _telemetry.Final.Winner)}"
            : "";
    }

    [RelayCommand]
    private void BackToCareer()
    {
        _timer?.Stop();
        _nav.NavigateTo<CareerViewModel>();
    }
}
