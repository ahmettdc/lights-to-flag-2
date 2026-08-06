using System.Linq;
using LTF.Domain;
using Xunit;

namespace LTF.Career.Tests;

public class SeasonProgressionTests
{
    // A progression that evolves nothing — the season must be byte-identical to the legacy Run.
    private sealed class NoProgression : IBetweenRounds
    {
        public Carset AfterRound(Carset current, BetweenRoundsContext context) => current;
    }

    [Fact]
    public void A_no_op_progression_matches_the_legacy_run()
    {
        var carset = CareerFixtures.SeasonCarset(rounds: 6);

        var legacy = SeasonSimulator.Run(carset, 2024);
        var progressed = SeasonSimulator.RunProgressed(carset, 2024, new NoProgression());

        Assert.Equal(Key(legacy), Key(progressed.Result));
    }

    [Fact]
    public void A_no_op_progression_leaves_the_carset_unchanged()
    {
        var carset = CareerFixtures.SeasonCarset(rounds: 4);

        var progressed = SeasonSimulator.RunProgressed(carset, 7, new NoProgression());

        Assert.Same(carset, progressed.Carset); // the seam is transparent when nothing evolves
    }

    [Fact]
    public void The_context_reports_the_round_position_and_the_next_date()
    {
        var carset = CareerFixtures.SeasonCarset(rounds: 3);
        var spy = new ContextSpy();

        SeasonSimulator.RunProgressed(carset, 7, spy);

        Assert.Equal(3, spy.Seen.Count);
        Assert.Equal(new[] { 0, 1, 2 }, spy.Seen.Select(c => c.RoundIndex).ToArray());
        Assert.All(spy.Seen, c => Assert.Equal(3, c.RoundCount));
        Assert.NotNull(spy.Seen[0].NextRoundDate);   // a next round follows
        Assert.Null(spy.Seen[2].NextRoundDate);      // the last round has none
    }

    [Fact]
    public void A_progressed_season_is_deterministic()
    {
        var carset = CareerFixtures.SeasonCarset(rounds: 5);

        var a = SeasonSimulator.RunProgressed(carset, 7, new NoProgression());
        var b = SeasonSimulator.RunProgressed(carset, 7, new NoProgression());

        Assert.Equal(Key(a.Result), Key(b.Result));
    }

    private sealed class ContextSpy : IBetweenRounds
    {
        public List<BetweenRoundsContext> Seen { get; } = [];

        public Carset AfterRound(Carset current, BetweenRoundsContext context)
        {
            Seen.Add(context);
            return current;
        }
    }

    private static string Key(SeasonResult r) =>
        string.Join(";", r.Standings.Drivers.Select(d => $"{d.Position},{d.DriverId},{d.Points},{d.Wins}")) + "|" +
        string.Join(";", r.Standings.Constructors.Select(c => $"{c.Position},{c.TeamId},{c.Points},{c.Wins}"));
}
