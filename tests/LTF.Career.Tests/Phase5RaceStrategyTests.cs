using System.Collections.Generic;
using System.Linq;
using LTF.Domain.Common;
using LTF.Domain.Racing;
using LTF.Simulation.Racing;
using Xunit;

namespace LTF.Career.Tests;

/// <summary>
/// Player pre-race strategy (M23b): <see cref="SeasonSimulator.RunRound"/> reads the carset's
/// <c>PlayerRaceStrategies</c> for the round it runs and feeds each chosen starting compound to the race
/// simulator. A carset with none runs exactly as before (byte-identical), and a choice for one round never
/// touches another — so setting a future round's strategy cannot rewrite a past race.
/// </summary>
public class Phase5RaceStrategyTests
{
    private static readonly IReadOnlyDictionary<string, int> NoPenalty = new Dictionary<string, int>();

    [Fact]
    public void Run_round_starts_a_driver_on_the_chosen_compound()
    {
        var carset = CareerFixtures.Carset() with
        {
            PlayerTeamId = "alpha",
            PlayerRaceStrategies = [new RaceStrategy { Round = 1, DriverId = "d1", Compound = TyreCompound.Soft }],
        };
        var round = carset.Calendar[0];

        var outcome = SeasonSimulator.RunRound(carset, round, 7, NoPenalty);

        var lap1 = outcome.Result.Telemetry.Laps[0].Order;
        Assert.Equal(TyreCompound.Soft, lap1.Single(o => o.CompetitorId == "d1").TyreCompound);
        // Everyone else starts on the field-wide default (Medium).
        Assert.All(lap1.Where(o => o.CompetitorId != "d1"), o => Assert.Equal(TyreCompound.Medium, o.TyreCompound));
    }

    [Fact]
    public void A_carset_without_strategies_runs_the_round_unchanged()
    {
        var carset = CareerFixtures.Carset();
        var round = carset.Calendar[0];

        var plain = SeasonSimulator.RunRound(carset, round, 7, NoPenalty);
        var emptyList = SeasonSimulator.RunRound(carset with { PlayerRaceStrategies = [] }, round, 7, NoPenalty);

        Assert.Equal(Key(plain.Result), Key(emptyList.Result));
        Assert.All(plain.Result.Telemetry.Laps[0].Order, o => Assert.Equal(TyreCompound.Medium, o.TyreCompound));
    }

    [Fact]
    public void A_strategy_for_another_round_leaves_this_round_unchanged()
    {
        var carset = CareerFixtures.Carset();
        var round = carset.Calendar[0]; // round 1

        var plain = SeasonSimulator.RunRound(carset, round, 7, NoPenalty);
        var future = SeasonSimulator.RunRound(
            carset with
            {
                PlayerRaceStrategies = [new RaceStrategy { Round = 2, DriverId = "d1", Compound = TyreCompound.Hard }],
            },
            round, 7, NoPenalty);

        Assert.Equal(Key(plain.Result), Key(future.Result)); // a round-2 choice does not touch round 1
    }

    private static string Key(RaceResult r) =>
        string.Join(";", r.Classification.Select(e => $"{e.Position},{e.CompetitorId},{e.Status},{e.TotalTime}"));
}
