using LTF.Domain;

namespace LTF.Career;

/// <summary>
/// The R&amp;D side of a season's between-rounds progression (M15): after each round it develops every team's
/// R&amp;D by that round's slice (<see cref="ResearchLedger.DevelopStep"/>), then adds a further development
/// pulse for each test day in the gap before the next round — so an upgrade approved mid-season is felt in
/// the season's later rounds. Deterministic (each round and test day seeds an independent stream); a no-op
/// for a carset with no tech tree, which keeps the season byte-identical. Each team's accumulated
/// <see cref="Developments"/> are read after the season, exactly as <see cref="ResearchLedger.DevelopSeason"/>
/// reports them.
/// </summary>
public sealed class RndProgression : IBetweenRounds
{
    private readonly int _seasonSeed;
    private readonly IDevelopmentDirectives? _directives;
    private readonly Dictionary<string, TeamDevelopment> _developments = new(StringComparer.Ordinal);
    private int _testOrdinal;

    public RndProgression(int seasonSeed, IDevelopmentDirectives? directives = null)
    {
        _seasonSeed = seasonSeed;
        _directives = directives;
    }

    /// <summary>Each team's development totalled across the season's rounds and test days.</summary>
    public IReadOnlyList<TeamDevelopment> Developments => _developments.Values.ToList();

    public Carset AfterRound(Carset current, BetweenRoundsContext context)
    {
        // This round's slice of the season's development, under the player's directive (if any).
        var outcome = ResearchLedger.DevelopStep(
            current, _seasonSeed, context.RoundIndex, context.RoundCount, _directives);
        Accumulate(outcome.Developments);
        current = outcome.Carset;

        // Any test day in the gap before the next round adds a further round-equivalent development pulse.
        // The first round's gap reaches back to the start of the season so a pre-season test counts once.
        var from = context.RoundIndex == 0 ? DateOnly.MinValue : context.Round.Date;
        var to = context.NextRoundDate ?? DateOnly.MaxValue;
        foreach (var testDay in current.TestDays)
        {
            if (testDay.Date > from && testDay.Date <= to)
            {
                var pulse = ResearchLedger.DevelopStep(
                    current, _seasonSeed, context.RoundCount + _testOrdinal, context.RoundCount, _directives);
                _testOrdinal++;
                Accumulate(pulse.Developments);
                current = pulse.Carset;
            }
        }

        return current;
    }

    private void Accumulate(IReadOnlyList<TeamDevelopment> developments)
    {
        foreach (var d in developments)
        {
            _developments[d.TeamId] = _developments.TryGetValue(d.TeamId, out var existing)
                ? new TeamDevelopment
                {
                    TeamId = d.TeamId,
                    BudgetSpent = existing.BudgetSpent + d.BudgetSpent,
                    NodesApproved = existing.NodesApproved + d.NodesApproved,
                    NodesAbandoned = existing.NodesAbandoned + d.NodesAbandoned,
                    ApprovedNodeIds = [.. existing.ApprovedNodeIds, .. d.ApprovedNodeIds],
                }
                : d;
        }
    }
}
