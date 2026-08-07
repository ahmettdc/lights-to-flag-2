using System.Collections.Generic;
using System.Linq;
using LTF.App.Notifications;
using LTF.Career;
using LTF.Domain;
using LTF.Domain.Racing;
using LTF.Domain.Rnd;
using LTF.Simulation.Racing;

namespace LTF.App.Session;

/// <summary>
/// The live career the shell drives (M21): a mutable holder over an immutable <see cref="ShellSession"/>.
/// A Football-Manager-style <see cref="Continue"/> advances the clock to the next dated event, runs the race
/// when it lands on a round, accumulates the results and recomputes the championship <see cref="Standings"/>.
///
/// Determinism / save-format: the save persists only the season-start carset + the date (unchanged from
/// M11/M20). Neither the standings nor the evolved car are persisted — both are <em>reconstructed</em>
/// deterministically from <c>(SeasonStart, Seed, Date)</c> by replaying every past round with the R&amp;D
/// progression threaded in, so a loaded mid-season save shows the same table — and the same evolved car — it
/// would after advancing there live. Component grid penalties are the season-scoped quantity keyed by round
/// (as <see cref="SeasonSimulator"/> does), so a full season of Continues reproduces
/// <see cref="SeasonSimulator.RunProgressed"/> exactly.
///
/// The dated inbox + action-required halt is M21e; the season-boundary rollover (M21f) evolves the world at
/// season end and opens the next season on the rolled carset. In-season R&amp;D development (Model B) evolves
/// <see cref="Current"/> round by round within a season, so it drifts from <see cref="SeasonStart"/> as the
/// car develops.
/// </summary>
public sealed class LiveCareer
{
    private static readonly IReadOnlyDictionary<string, int> NoPenalty =
        new Dictionary<string, int>(System.StringComparer.Ordinal);

    // A fixed salt mixed with the season seed for the winter R&D pulse, so its draws never collide with a
    // round's race seed.
    private const int WinterSeedSalt = 0x7157;

    private readonly List<RaceResult> _results = new();
    private readonly Dictionary<int, IReadOnlyDictionary<string, int>> _penaltyByRound = new();
    private readonly Dictionary<int, CalendarRound> _roundByNumber = new();
    private readonly Dictionary<int, int> _indexByRound = new();
    private RndProgression _progression = null!;
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

    /// <summary>Apply a player decision to the career (M22c) — e.g. taking a loan. The transform lands on the
    /// <see cref="SeasonStart"/> carset (the save/reconstruction base) and is mirrored to <see cref="Current"/>,
    /// so the change persists through save/load and shows immediately when the open screen rebuilds. Standings
    /// are re-derived, which is a no-op for finance/staff/research edits (they never feed the race sim), so one
    /// method serves every mid-season mutation while the reconstruction invariant holds.</summary>
    public void ApplyToSeasonStart(System.Func<Carset, Carset> transform)
    {
        SeasonStart = transform(SeasonStart);
        Current = SeasonStart;
        RebuildRoundMaps();
        Reconstruct();
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
                    var result = RunAndEvolve(round, _indexByRound[round.Round]);
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

    // Run one round on the live (evolving) carset, then evolve it for the remaining rounds via the R&D
    // progression — mirroring SeasonSimulator.RunCore so a live season reproduces RunProgressed exactly
    // (Model B: the car develops mid-season). The grid penalty stays season-scoped (from SeasonStart), never
    // recomputed from the evolved carset, so byte-identity holds. AfterRound is called after every round
    // (including the last), matching RunCore, and evolves the carset the next round is contested with.
    private RaceResult RunAndEvolve(CalendarRound round, int roundIndex)
    {
        var penalty = _penaltyByRound.GetValueOrDefault(round.Round, NoPenalty);
        var outcome = SeasonSimulator.RunRound(Current, round, SeasonSimulator.RoundSeed(Seed, round.Round), penalty);
        _results.Add(outcome.Result);

        System.DateOnly? nextDate = roundIndex + 1 < SeasonStart.Calendar.Count
            ? SeasonStart.Calendar[roundIndex + 1].Date
            : null;
        Current = _progression.AfterRound(Current, new BetweenRoundsContext
        {
            Round = round,
            RoundIndex = roundIndex,
            RoundCount = SeasonStart.Calendar.Count,
            NextRoundDate = nextDate,
            Seed = SeasonSimulator.RoundSeed(Seed, round.Round),
        });

        return outcome.Result;
    }

    // The player team's development directive: its concept lean steers node choices (neutral is a no-op, so an
    // all-AI or unset carset develops byte-identically to no directive). Read fresh from SeasonStart, so a
    // concept change (Ri2) re-steers the whole reconstructed season.
    private IDevelopmentDirectives PlayerDirective() =>
        new RndDirection(SeasonStart.PlayerTeamId, SeasonStart.PlayerTeam()?.Research.Concept ?? ConceptDirection.Neutral);

    /// <summary>Cross a season boundary (M21f + M22a): settle the season just played and evolve the world.
    /// First the management settle chain — economy, bank loans, enforcement (icra) and the board review,
    /// mirroring <see cref="BossCareerSweep"/>/<see cref="BankSweep"/> — then the M18 <see cref="WorldSweep"/>
    /// evolution (relationships, aging/retirement, transfers, contracts, regulation), so
    /// <see cref="CareerRollover"/> rides the settled finances. A defaulting loan raises an action-required
    /// enforcement item that halts Continue (Rev 15). Deterministic in the career seed; the rolled carset
    /// (settled finances + evolved boards baked in) becomes the new save base, so a load resumes with no re-roll.</summary>
    private void RollToNextSeason()
    {
        // The canonical result of the season just played — reproduced with mid-season R&D threaded in (Model B),
        // so it matches what the live Continues produced: the car evolves round by round via the progression.
        var rnd = new RndProgression(Seed, PlayerDirective());
        var progress = SeasonSimulator.RunProgressed(SeasonStart, Seed, rnd);
        var result = progress.Result;
        var seasonSeed = SeasonSimulator.RoundSeed(Seed, _seasonIndex);
        var seatTargets = SeasonStart.Teams.ToDictionary(t => t.Id, t => t.DriverIds.Count, System.StringComparer.Ordinal);

        // Winter development (Ri3): axes an FIA regulation freezes to "in-season only" were held back all season;
        // at the boundary they get their season's development in one pulse (fully-frozen axes stay frozen).
        // Inert (returns progress.Carset unchanged) when no such freeze is active, so a freeze-free career rolls
        // over byte-identically.
        var winter = ResearchLedger.DevelopWinter(
            progress.Carset, SeasonSimulator.RoundSeed(seasonSeed, WinterSeedSalt), PlayerDirective());

        // Management settle chain — economy → loans → enforcement → board — before the world evolution, so the
        // settled finances/boards are what CareerRollover and the save base carry forward. Economy settles on the
        // evolved end-of-season carset; the season's total R&D spend (mid-season + winter) is folded into the
        // cost-cap check (already debited from the balance during development, so it is not re-charged).
        var rndSpend = new Dictionary<string, long>(System.StringComparer.Ordinal);
        foreach (var d in rnd.Developments)
        {
            rndSpend[d.TeamId] = d.BudgetSpent;
        }

        foreach (var d in winter.Developments)
        {
            rndSpend[d.TeamId] = rndSpend.GetValueOrDefault(d.TeamId) + d.BudgetSpent;
        }

        var settlement = EconomyLedger.SettleSeason(winter.Carset, result, rndSpend);
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
        _indexByRound.Clear();

        // Component grid penalties are season-scoped: computed once from the season-start carset and keyed by
        // round number, so a round always draws the same penalty whether it runs live or on reconstruction.
        var penalties = ComponentPenalties.ForSeason(SeasonStart);
        for (var i = 0; i < SeasonStart.Calendar.Count; i++)
        {
            var round = SeasonStart.Calendar[i];
            _roundByNumber[round.Round] = round;
            _penaltyByRound[round.Round] = penalties[i];
            _indexByRound[round.Round] = i;
        }
    }

    /// <summary>Rebuild results + standings + the evolved carset for the current date: replay every past round
    /// from the season-start carset, threading the R&amp;D progression so the car evolves mid-season exactly as it
    /// did live (Model B). Deterministic in <c>(SeasonStart, Seed, Date)</c>, so a load reproduces the live
    /// table <em>and</em> the live car with no persisted standings or car — the save format is unchanged. The
    /// progression instance is retained so the next live Continue resumes its state.</summary>
    private void Reconstruct()
    {
        _results.Clear();
        _progression = new RndProgression(Seed, PlayerDirective());
        Current = SeasonStart;

        for (var i = 0; i < SeasonStart.Calendar.Count; i++)
        {
            var round = SeasonStart.Calendar[i];
            if (round.Date <= Clock.Date)
            {
                RunAndEvolve(round, i);
            }
        }

        Standings = ChampionshipStandings.From(SeasonStart, _results);
    }
}
