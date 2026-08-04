using LTF.Domain.Common;
using LTF.Domain.Racing;
using LTF.Simulation.Laps;

namespace LTF.Simulation.Racing;

/// <summary>
/// Runs a race lap by lap and produces a classification plus telemetry. Fully
/// deterministic: every car draws from its own stream forked off the race seed, so the
/// same seed and grid reproduce the same race bit for bit. M5a is the pace-only skeleton —
/// reliability, driver errors, collisions and neutralisations arrive in M5b–M5e.
/// </summary>
public static class RaceSimulator
{
    public static RaceResult Run(
        Circuit circuit, IReadOnlyList<Competitor> grid, RulesSet rules, BalanceCoefficients balance,
        int seed, TyreCompound startingCompound = TyreCompound.Medium)
    {
        var baseRng = new DeterministicRandom(seed);
        var laps = circuit.Laps;

        var cars = new List<CarRaceState>(grid.Count);
        for (var i = 0; i < grid.Count; i++)
        {
            cars.Add(new CarRaceState(
                grid[i], gridPosition: i + 1, rng: baseRng.Fork(i + 1),
                tyre: TyreState.Fresh(startingCompound), fuel: 1.0));
        }

        var snapshots = new List<LapSnapshot>(laps);

        for (var lap = 1; lap <= laps; lap++)
        {
            foreach (var car in cars)
            {
                if (!car.Running)
                {
                    continue;
                }

                var conditions = new LapConditions
                {
                    Tyre = car.Tyre,
                    FuelFraction = car.Fuel,
                    Track = TrackConditions.Dry,
                };

                var lapTime = LapTimeModel.Simulate(circuit, car.Competitor, balance, conditions, car.Rng).Total;

                car.TotalTime += lapTime;
                car.LapsCompleted = lap;
                if (lapTime < car.BestLap)
                {
                    car.BestLap = lapTime;
                }

                car.Tyre = TyreModel.Advance(car.Tyre, circuit, car.Competitor.Driver.Attributes, balance);
                car.Fuel = FuelModel.Burn(car.Fuel, laps);
            }

            snapshots.Add(SnapshotOf(lap, cars));
        }

        return Classify(cars, rules, snapshots);
    }

    private static LapSnapshot SnapshotOf(int lap, List<CarRaceState> cars)
    {
        var running = cars
            .Where(c => c.Running)
            .OrderByDescending(c => c.LapsCompleted)
            .ThenBy(c => c.TotalTime)
            .ToList();

        var leaderTime = running.Count > 0 ? running[0].TotalTime : 0.0;
        var order = new List<LapStanding>(running.Count);
        for (var i = 0; i < running.Count; i++)
        {
            var c = running[i];
            order.Add(new LapStanding(c.Id, i + 1, c.TotalTime, c.TotalTime - leaderTime));
        }

        return new LapSnapshot { Lap = lap, Order = order };
    }

    private static RaceResult Classify(List<CarRaceState> cars, RulesSet rules, List<LapSnapshot> snapshots)
    {
        var ordered = cars
            .OrderByDescending(c => c.Status == FinishStatus.Finished)
            .ThenByDescending(c => c.LapsCompleted)
            .ThenBy(c => c.TotalTime)
            .ToList();

        CarRaceState? fastest = null;
        foreach (var c in cars)
        {
            if (c.BestLap < double.MaxValue && (fastest is null || c.BestLap < fastest.BestLap))
            {
                fastest = c;
            }
        }

        var leaderTime = ordered.Count > 0 ? ordered[0].TotalTime : 0.0;
        var entries = new List<RaceClassificationEntry>(ordered.Count);
        for (var i = 0; i < ordered.Count; i++)
        {
            var c = ordered[i];
            var position = i + 1;

            var points = c.Status == FinishStatus.Finished ? rules.Points.PointsFor(position) : 0;
            if (rules.Points.FastestLapPoint > 0 && fastest is not null && c.Id == fastest.Id
                && c.Status == FinishStatus.Finished && position <= 10)
            {
                points += rules.Points.FastestLapPoint;
            }

            entries.Add(new RaceClassificationEntry
            {
                Position = position,
                CompetitorId = c.Id,
                Status = c.Status,
                RetirementReason = c.RetirementReason,
                Laps = c.LapsCompleted,
                TotalTime = c.TotalTime,
                GapToLeader = c.TotalTime - leaderTime,
                BestLap = c.BestLap < double.MaxValue ? c.BestLap : 0.0,
                Points = points,
            });
        }

        return new RaceResult
        {
            Classification = entries,
            Telemetry = new RaceTelemetry { Laps = snapshots },
            FastestLapCompetitorId = fastest?.Id,
            FastestLapTime = fastest?.BestLap ?? 0.0,
        };
    }
}
