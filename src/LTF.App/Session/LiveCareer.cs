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
/// The dated inbox + action-required halt is M21e; the season-boundary rollover (M21f) evolves the world at
/// season end and opens the next season on the rolled carset. In-season carset evolution (R&amp;D/test days)
/// is not applied yet, so <see cref="Current"/> equals <see cref="SeasonStart"/> within a season.
/// </summary>
public sealed class LiveCareer
{
    private static readonly IReadOnlyDictionary<string, int> NoPenalty =
        new Dictionary<string, int>(System.StringComparer.Ordinal);

    private readonly List<RaceResult> _results = new();
    private readonly Dictionary<int, IReadOnlyDictionary<string, int>> _penaltyByRound = new();
    private readonly Dictionary<int, CalendarRound> _roundByNumber = new();
    private int _seasonIndex;

    public LiveCareer(ShellSession session)
    {
        SeasonStart = session.Carset;
        Current = session.Carset;
        Seed = session.Seed;
        Clock = session.Clock;

        RebuildRoundMaps();
        Standings = ChampionshipStandings.Empty(SeasonStart);
        Reconstruct();
    }

    /// <summary>The current season's opening carset — the deterministic reconstruction base (advances at a
    /// season boundary to the rolled carset).</summary>
    public Carset SeasonStart { get; private set; }

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

    /// <summary>Whether a plain <see cref="Continue"/> can advance now: the career is endless (Continue rolls
    /// into the next season at a boundary), so it is blocked only by an unacknowledged action-required item
    /// (Rev 15). Clear a pending item with <see cref="Acknowledge"/>.</summary>
    public bool CanContinue => !PendingAction;

    /// <summary>True at a season boundary — the current season's events are exhausted and the next Continue
    /// rolls the world into a new season.</summary>
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

        // At a season boundary, Continue rolls the world into the next season (M21f) rather than stalling.
        if (Clock.SeasonComplete)
        {
            RollToNextSeason();
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

    /// <summary>Cross a season boundary (M21f + M22a): settle the season just played and evolve the world.
    /// First the management settle chain — economy, bank loans, enforcement (icra) and the board review,
    /// mirroring <see cref="BossCareerSweep"/>/<see cref="BankSweep"/> — then the M18 <see cref="WorldSweep"/>
    /// evolution (relationships, aging/retirement, transfers, contracts, regulation), so
    /// <see cref="CareerRollover"/> rides the settled finances. A defaulting loan raises an action-required
    /// enforcement item that halts Continue (Rev 15). Deterministic in the career seed; the rolled carset
    /// (settled finances + evolved boards baked in) becomes the new save base, so a load resumes with no re-roll.</summary>
    private void RollToNextSeason()
    {
        // The canonical result of the season just played (LiveCareer reproduces SeasonSimulator.Run exactly).
        var result = SeasonSimulator.Run(SeasonStart, Seed);
        var seasonSeed = SeasonSimulator.RoundSeed(Seed, _seasonIndex);
        var seatTargets = SeasonStart.Teams.ToDictionary(t => t.Id, t => t.DriverIds.Count, System.StringComparer.Ordinal);

        // Management settle chain — economy → loans → enforcement → board — before the world evolution, so the
        // settled finances/boards are what CareerRollover and the save base carry forward.
        var settlement = EconomyLedger.SettleSeason(SeasonStart, result);
        var banked = BankLedger.SettleSeason(settlement.Carset);
        var enforcement = BankEnforcement.Enforce(banked.Carset, banked.Missed);
        var penalties = settlement.Penalties.Concat(enforcement.PointsPenalties).ToList();
        var judged = ConstructorPenalties.Apply(result.Standings, penalties);
        var settled = BoardReview.Assess(enforcement.Carset, judged, seasonSeed);

        var next = settled;
        next = RelationshipEvolution.Apply(next, result);          // incidents move the paddock
        next = CareerRollover.Apply(next, result);                 // roll the season into records
        next = DriverProgression.Advance(next, seasonSeed);        // age, grow and decline
        next = DriverRetirement.Retire(next);                      // the over-age leave, seats open
        next = ContractLedger.AdvanceSeason(next);                 // contracts count down
        next = TransferMarket.Resolve(next, seatTargets, result.Standings, seasonSeed); // fill seats
        next = RegulationChange.Apply(next, seasonSeed);           // set back the unprepared

        _seasonIndex++;
        SeasonStart = next;
        Current = next;
        Clock = CareerClock.Start(next);
        RebuildRoundMaps();
        _results.Clear();
        Standings = ChampionshipStandings.Empty(next);

        // The new-season item, plus one action-required enforcement item per defaulting team (the player, in
        // practice) so Continue halts on a missed loan (Rev 15).
        var news = new List<Notification> { CareerNews.ForNewSeason(Clock.Date, _seasonIndex) };
        foreach (var action in enforcement.Actions)
        {
            news.Add(CareerNews.ForEnforcement(Clock.Date, action, next));
        }

        LastStepNews = news;
        PendingAction = news.Any(n => n.RequiresAction);
    }

    private void RebuildRoundMaps()
    {
        _roundByNumber.Clear();
        _penaltyByRound.Clear();

        // Component grid penalties are season-scoped: computed once from the season-start carset and keyed by
        // round number, so a round always draws the same penalty whether it runs live or on reconstruction.
        var penalties = ComponentPenalties.ForSeason(SeasonStart);
        for (var i = 0; i < SeasonStart.Calendar.Count; i++)
        {
            var round = SeasonStart.Calendar[i];
            _roundByNumber[round.Round] = round;
            _penaltyByRound[round.Round] = penalties[i];
        }
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
