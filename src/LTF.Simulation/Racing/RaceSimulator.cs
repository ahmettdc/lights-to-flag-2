using LTF.Domain.Common;
using LTF.Domain.Racing;
using LTF.Simulation.Laps;

namespace LTF.Simulation.Racing;

/// <summary>
/// Runs a race lap by lap and produces a classification, an event log and telemetry. Fully
/// deterministic: every car draws pace from its own stream forked off the race seed, and
/// reliability from a second stream forked off that, so the same seed and grid reproduce the
/// same race bit for bit. M5b adds component health, engine modes and mechanical failures on
/// top of the M5a pace skeleton; driver errors, collisions and neutralisations arrive in M5c–M5e.
/// </summary>
public static class RaceSimulator
{
    // A car's reliability stream is forked off its pace stream with this fixed salt, so pace
    // draws are byte-identical to a race with no event model at all.
    private const long ReliabilitySalt = 0x5245_4C49; // "RELI"

    // Below this health the weakest component starts costing lap time (limp mode).
    private const double LimpThreshold = 0.30;

    // How sharply failure risk climbs as a component wears out (added at zero health).
    private const double LowHealthRiskMultiplier = 5.0;

    private static readonly ComponentKind[] Components = Enum.GetValues<ComponentKind>();

    public static RaceResult Run(
        Circuit circuit, IReadOnlyList<Competitor> grid, RulesSet rules, BalanceCoefficients balance,
        int seed, TyreCompound startingCompound = TyreCompound.Medium)
    {
        var baseRng = new DeterministicRandom(seed);
        var laps = circuit.Laps;

        var cars = new List<CarRaceState>(grid.Count);
        for (var i = 0; i < grid.Count; i++)
        {
            var paceRng = baseRng.Fork(i + 1);
            cars.Add(new CarRaceState(
                grid[i], gridPosition: i + 1, rng: paceRng, reliabilityRng: paceRng.Fork(ReliabilitySalt),
                tyre: TyreState.Fresh(startingCompound), fuel: 1.0,
                health: ComponentHealth.Fresh(), mode: EngineMode.Standard));
        }

        var snapshots = new List<LapSnapshot>(laps);
        var events = new List<RaceEvent>();

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

                // Pace first: draws happen up front so the pace stream is identical whatever
                // the reliability model does this lap.
                var lapTime = LapTimeModel.Simulate(circuit, car.Competitor, balance, conditions, car.Rng).Total
                              + EngineModes.PaceDelta(car.Mode, balance)
                              + LimpPenalty(car.Health, balance);

                // Wear the car, then see if anything lets go this lap (reliability stream).
                DegradeHealth(car, balance);
                if (TryFail(car, lap, balance, events))
                {
                    // Retired mid-lap: it does not count as a completed lap.
                    continue;
                }

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

        return Classify(cars, rules, snapshots, events);
    }

    /// <summary>Extra lap time from nursing a badly worn car home; 0 until the weakest
    /// component drops below the limp threshold, then growing as it approaches zero.</summary>
    private static double LimpPenalty(ComponentHealth health, BalanceCoefficients balance)
    {
        var lowest = health.Lowest;
        if (lowest >= LimpThreshold)
        {
            return 0.0;
        }

        return balance.LimpPaceLossSeconds * ((LimpThreshold - lowest) / LimpThreshold);
    }

    /// <summary>Wear every component this lap. Reliable cars wear slower; a harder engine
    /// mode wears faster.</summary>
    private static void DegradeHealth(CarRaceState car, BalanceCoefficients balance)
    {
        var reliabilityNorm = car.Competitor.Car.Reliability.Normalized;
        var wearFactor = 1.5 - reliabilityNorm; // reliability 100 → 0.5×, reliability 1 → ~1.5×
        var loss = balance.ComponentHealthLossPerLap * wearFactor * EngineModes.RiskFactor(car.Mode, balance);

        foreach (var kind in Components)
        {
            car.Health.Degrade(kind, loss);
        }
    }

    /// <summary>Roll each component for a terminal failure this lap. Returns true (and retires
    /// the car, recording the event) on the first component that lets go.</summary>
    private static bool TryFail(CarRaceState car, int lap, BalanceCoefficients balance, List<RaceEvent> events)
    {
        var reliabilityNorm = car.Competitor.Car.Reliability.Normalized;
        var reliabilityFactor = 1.0 - (0.7 * reliabilityNorm); // reliability 100 → 0.3×, reliability 1 → ~1.0×
        var modeRisk = EngineModes.RiskFactor(car.Mode, balance);

        foreach (var kind in Components)
        {
            var health = car.Health.Of(kind);
            var lowHealthMult = 1.0 + ((1.0 - health) * LowHealthRiskMultiplier);
            var chance = balance.ReliabilityFailureRate * reliabilityFactor * lowHealthMult * modeRisk;

            if (car.ReliabilityRng.NextDouble() < chance)
            {
                var reason = Describe(kind);
                car.Running = false;
                car.Status = FinishStatus.Retired;
                car.RetirementReason = reason;
                events.Add(new RaceEvent
                {
                    Kind = RaceEventKind.MechanicalFailure,
                    Lap = lap,
                    CompetitorId = car.Id,
                    Description = reason,
                });
                return true;
            }
        }

        return false;
    }

    private static string Describe(ComponentKind kind) => kind switch
    {
        ComponentKind.Engine => "Engine failure",
        ComponentKind.Gearbox => "Gearbox failure",
        ComponentKind.Brakes => "Brake failure",
        _ => "Mechanical failure",
    };

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

    private static RaceResult Classify(
        List<CarRaceState> cars, RulesSet rules, List<LapSnapshot> snapshots, List<RaceEvent> events)
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
            Events = events,
            FastestLapCompetitorId = fastest?.Id,
            FastestLapTime = fastest?.BestLap ?? 0.0,
        };
    }
}
