using System.Collections.Generic;
using System.Linq;
using LTF.App.Notifications;
using LTF.Career;
using LTF.Domain;
using LTF.Domain.Racing;
using LTF.Simulation.Racing;

namespace LTF.App.Session;

/// <summary>
/// The live career the shell drives (M21): a mutable holder over an immutable <see cref="ShellSession"/>.
/// A Football-Manager-style <see cref="Continue"/> advances the clock to the next dated event, runs the race
/// when it lands on a round, accumulates the results and recomputes the championship <see cref="Standings"/>.
///
/// Determinism / save-format: the save persists only the season-start carset + the date (unchanged from
/// M11/M20). Standings are never persisted — they are <em>reconstructed</em> deterministically from
/// <c>(SeasonStart, Seed, Date)</c> by re-running every round already in the past, so a loaded mid-season
/// save shows the same table it would after advancing there live. Component grid penalties are the
/// season-scoped quantity keyed by round (as <see cref="SeasonSimulator"/> does), so a full season of
/// Continues reproduces <see cref="SeasonSimulator.Run"/> exactly.
///
/// M21d runs the in-season rounds only; the dated inbox + action-required halt (M21e) and the season-boundary
/// rollover (M21f) build on this. In-season carset evolution (R&amp;D/test days) is not applied yet, so
/// <see cref="Current"/> equals <see cref="SeasonStart"/> for now.
/// </summary>
public sealed class LiveCareer
{
    private static readonly IReadOnlyDictionary<string, int> NoPenalty =
        new Dictionary<string, int>(System.StringComparer.Ordinal);

    private readonly List<RaceResult> _results = new();
    private readonly Dictionary<int, IReadOnlyDictionary<string, int>> _penaltyByRound = new();
    private readonly Dictionary<int, CalendarRound> _roundByNumber = new();

    public LiveCareer(ShellSession session)
    {
        SeasonStart = session.Carset;
        Current = session.Carset;
        Seed = session.Seed;
        Clock = session.Clock;

        // Component grid penalties are season-scoped: computed once from the season-start carset and keyed
        // by round number, so a round always draws the same penalty whether it runs live or on reconstruction.
        var penalties = ComponentPenalties.ForSeason(SeasonStart);
        for (var i = 0; i < SeasonStart.Calendar.Count; i++)
        {
            var round = SeasonStart.Calendar[i];
            _roundByNumber[round.Round] = round;
            _penaltyByRound[round.Round] = penalties[i];
        }

        Standings = ChampionshipStandings.Empty(SeasonStart);
        Reconstruct();
    }

    /// <summary>The season's opening carset — the deterministic reconstruction base.</summary>
    public Carset SeasonStart { get; }

    /// <summary>The carset the shell reads today (M21d: equals <see cref="SeasonStart"/>).</summary>
    public Carset Current { get; private set; }

    public int Seed { get; }

    public CareerClock Clock { get; private set; }

    public Standings Standings { get; private set; }

    public IReadOnlyList<RaceResult> Results => _results;

    /// <summary>The session as it stands now (current carset + clock), for the screens/top bar.</summary>
    public ShellSession Session => new(Current, Clock, Seed);

    /// <summary>The session to persist: the season-start carset + current date (never the standings), so the
    /// save format is unchanged and a load reconstructs.</summary>
    public ShellSession SaveSession => new(SeasonStart, Clock, Seed);

    /// <summary>Whether a plain <see cref="Continue"/> can advance now: the season isn't over and no
    /// action-required item is pending (Rev 15). Clear a pending item with <see cref="Acknowledge"/>.</summary>
    public bool CanContinue => !Clock.SeasonComplete && !PendingAction;

    /// <summary>True once the season's events are exhausted (Continue has nowhere left to go).</summary>
    public bool SeasonComplete => Clock.SeasonComplete;

    /// <summary>Set when the last step surfaced an action-required event (a contract deadline): Continue
    /// pauses until it is acknowledged (Rev 15). Transient — never persisted.</summary>
    public bool PendingAction { get; private set; }

    /// <summary>The dated notifications the last <see cref="Continue"/> raised, for the inbox feed.</summary>
    public IReadOnlyList<Notification> LastStepNews { get; private set; } = [];

    /// <summary>Acknowledge a pending action so Continue can advance again (Rev 15).</summary>
    public void Acknowledge()
    {
        PendingAction = false;
        LastStepNews = [];
    }

    /// <summary>Advance one Football-Manager "Continue": jump the clock to the next event's date and dispatch
    /// every event falling on it — running the race for a round weekend, folding it into the standings, and
    /// raising a dated inbox item per event. Halts (sets <see cref="PendingAction"/>) when an event needs the
    /// player to act. No-op while a pending action is unacknowledged or the season is complete.</summary>
    public void Continue()
    {
        if (!CanContinue)
        {
            return;
        }

        Clock = Clock.ContinueToNextEvent();

        var news = new List<Notification>();
        foreach (var todaysEvent in Clock.Today)
        {
            switch (todaysEvent.Kind)
            {
                case CalendarEventKind.RaceWeekend when _roundByNumber.TryGetValue(todaysEvent.Round, out var round):
                    var result = RunRound(round, todaysEvent.Round);
                    news.Add(CareerNews.ForRace(Clock.Date, round, result, Current));
                    break;

                case CalendarEventKind.ContractDeadline:
                    news.Add(CareerNews.ForContractDeadline(Clock.Date, todaysEvent, Current));
                    break;

                case CalendarEventKind.BoardReview:
                    news.Add(CareerNews.ForBoardReview(Clock.Date));
                    break;

                // TestDay / RegulationAnnouncement / TransferWindow raise no inbox item in M21.
            }
        }

        Standings = ChampionshipStandings.From(SeasonStart, _results);
        LastStepNews = news;
        PendingAction = news.Any(n => n.RequiresAction);
    }

    private RaceResult RunRound(CalendarRound round, int roundNumber)
    {
        var penalty = _penaltyByRound.GetValueOrDefault(roundNumber, NoPenalty);
        var outcome = SeasonSimulator.RunRound(Current, round, SeasonSimulator.RoundSeed(Seed, roundNumber), penalty);
        _results.Add(outcome.Result);
        return outcome.Result;
    }

    /// <summary>Rebuild results + standings for the current date: re-run every round already in the past from
    /// the season-start carset. Deterministic, so it matches advancing there live with no persisted standings.</summary>
    private void Reconstruct()
    {
        _results.Clear();

        foreach (var round in SeasonStart.Calendar.Where(r => r.Date <= Clock.Date).OrderBy(r => r.Round))
        {
            var penalty = _penaltyByRound.GetValueOrDefault(round.Round, NoPenalty);
            var outcome = SeasonSimulator.RunRound(SeasonStart, round, SeasonSimulator.RoundSeed(Seed, round.Round), penalty);
            _results.Add(outcome.Result);
        }

        Standings = ChampionshipStandings.From(SeasonStart, _results);
    }
}
