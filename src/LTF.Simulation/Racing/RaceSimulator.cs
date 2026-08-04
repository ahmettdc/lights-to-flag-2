using LTF.Domain.Common;
using LTF.Domain.Racing;
using LTF.Simulation.Laps;

namespace LTF.Simulation.Racing;

/// <summary>
/// Runs a race lap by lap and produces a classification, an event log and telemetry. Fully
/// deterministic: every car draws pace, reliability and incidents from separate streams
/// forked off the race seed, plus a race-control stream for neutralisations, so the same
/// seed and grid reproduce the same race bit for bit. M5a laid the pace skeleton, M5b added
/// reliability, M5c the start / driver errors / collisions, and M5d the safety car, VSC and
/// red-flag neutralisations.
/// </summary>
public static class RaceSimulator
{
    private const long ReliabilitySalt = 0x5245_4C49; // "RELI"
    private const long IncidentSalt = 0x494E_4344;    // "INCD"
    private const long RaceControlSalt = 0x5241_4345; // "RACE"

    // Below this health the weakest component starts costing lap time (limp mode).
    private const double LimpThreshold = 0.30;

    // How sharply failure risk climbs as a component wears out (added at zero health).
    private const double LowHealthRiskMultiplier = 5.0;

    // Share of driver errors / collisions severe enough to end a car's race.
    private const double DriverErrorCrashShare = 0.12;
    private const double CollisionHeavyShare = 0.20;

    private static readonly ComponentKind[] Components = Enum.GetValues<ComponentKind>();

    public static RaceResult Run(
        Circuit circuit, IReadOnlyList<Competitor> grid, RulesSet rules, BalanceCoefficients balance,
        int seed, TyreCompound startingCompound = TyreCompound.Medium)
    {
        var baseRng = new DeterministicRandom(seed);
        var raceControlRng = baseRng.Fork(RaceControlSalt);
        var laps = circuit.Laps;

        var cars = new List<CarRaceState>(grid.Count);
        for (var i = 0; i < grid.Count; i++)
        {
            var paceRng = baseRng.Fork(i + 1);
            cars.Add(new CarRaceState(
                grid[i], gridPosition: i + 1, rng: paceRng,
                reliabilityRng: paceRng.Fork(ReliabilitySalt), incidentRng: paceRng.Fork(IncidentSalt),
                tyre: TyreState.Fresh(startingCompound), fuel: 1.0,
                health: ComponentHealth.Fresh(), mode: EngineMode.Standard));
        }

        var snapshots = new List<LapSnapshot>(laps);
        var events = new List<RaceEvent>();

        // Lights out: start performance and any getaway trouble, before lap one.
        foreach (var car in cars)
        {
            ApplyStart(car, balance, events);
        }

        var state = NeutralizationState.Green;
        var neutralLapsLeft = 0;

        for (var lap = 1; lap <= laps; lap++)
        {
            var stateThisLap = state;
            var neutralized = stateThisLap != NeutralizationState.Green;
            var aheadOf = AheadMap(cars);
            var retiredThisLap = 0;
            string? triggerCarId = null;

            foreach (var car in cars)
            {
                if (!car.Running)
                {
                    continue;
                }

                if (neutralized)
                {
                    // Circulating behind the safety car: a slow, uniform lap, no racing.
                    car.TotalTime += NeutralizedLapTime(circuit, balance);
                    car.LapsCompleted = lap;
                    car.Fuel = FuelModel.Burn(car.Fuel, laps);
                    continue;
                }

                var conditions = new LapConditions
                {
                    Tyre = car.Tyre,
                    FuelFraction = car.Fuel,
                    Track = TrackConditions.Dry,
                };

                // Pace first: draws happen up front so the pace stream is identical whatever
                // the reliability and incident models do this lap.
                var lapTime = LapTimeModel.Simulate(circuit, car.Competitor, balance, conditions, car.Rng).Total
                              + EngineModes.PaceDelta(car.Mode, balance)
                              + LimpPenalty(car.Health, balance);

                // Reliability (its own stream): wear the car, then roll each component.
                DegradeHealth(car, balance);
                if (TryFail(car, lap, balance, events))
                {
                    retiredThisLap++;
                    triggerCarId = car.Id;
                    continue;
                }

                // Incidents (its own stream): a driver error, then contact with the car ahead.
                lapTime += ApplyDriverError(car, lap, conditions, balance, events);
                if (!car.Running)
                {
                    retiredThisLap++;
                    triggerCarId = car.Id;
                    continue;
                }

                lapTime += ApplyCollision(car, aheadOf.GetValueOrDefault(car.Id), lap, balance, events);
                if (!car.Running)
                {
                    retiredThisLap++;
                    triggerCarId = car.Id;
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

            // Decide the next state before snapshotting, so a restart's bunching is captured now.
            NeutralizationState nextState;
            int nextLeft;
            if (neutralized)
            {
                nextLeft = neutralLapsLeft - 1;
                if (nextLeft <= 0)
                {
                    ApplyRestart(cars, stateThisLap, balance, startingCompound);
                    nextState = NeutralizationState.Green;
                    nextLeft = 0;
                }
                else
                {
                    nextState = stateThisLap;
                }
            }
            else
            {
                nextState = NeutralizationState.Green;
                nextLeft = 0;
                if (retiredThisLap > 0 && TryTriggerNeutralization(raceControlRng, circuit, balance, retiredThisLap))
                {
                    nextState = ChooseNeutralization(raceControlRng, balance);
                    nextLeft = balance.NeutralizationLaps;
                    events.Add(new RaceEvent
                    {
                        Kind = ToEventKind(nextState),
                        Lap = lap,
                        CompetitorId = triggerCarId ?? string.Empty,
                        Description = NeutralizationLabel(nextState),
                    });
                }
            }

            snapshots.Add(SnapshotOf(lap, cars, stateThisLap));
            state = nextState;
            neutralLapsLeft = nextLeft;
        }

        return Classify(cars, rules, snapshots, events);
    }

    // ---- Start ------------------------------------------------------------

    /// <summary>Bake start performance into the car's time before lap one: a skill-based loss
    /// off an ideal getaway plus random spread, and a rare jump start that draws a penalty.</summary>
    private static void ApplyStart(CarRaceState car, BalanceCoefficients balance, List<RaceEvent> events)
    {
        var attr = car.Competitor.Driver.Attributes;
        var quality = (0.5 * attr.Racecraft.Normalized) + (0.5 * attr.Consistency.Normalized);
        var loss = balance.StartSkillSeconds * (1.0 - quality);
        var jitter = car.IncidentRng.NextGaussian() * balance.StartSpreadSeconds;
        car.TotalTime += Math.Max(0.0, loss + jitter);

        if (car.IncidentRng.NextDouble() < balance.StartIncidentRate)
        {
            car.TotalTime += balance.StartIncidentPenaltySeconds;
            events.Add(new RaceEvent
            {
                Kind = RaceEventKind.StartIncident,
                Lap = 0,
                CompetitorId = car.Id,
                Description = "Jump start (penalty)",
            });
        }
    }

    // ---- Reliability (M5b) ------------------------------------------------

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

    // ---- Incidents (M5c) --------------------------------------------------

    /// <summary>Roll for a driver error this lap. A bad enough one ends the race; otherwise it
    /// costs time. Inconsistent drivers err more; wet weather and lap one raise the odds; good
    /// racecraft keeps an error out of the wall.</summary>
    private static double ApplyDriverError(
        CarRaceState car, int lap, LapConditions conditions, BalanceCoefficients balance, List<RaceEvent> events)
    {
        var attr = car.Competitor.Driver.Attributes;
        var proneness = 1.0 - attr.Consistency.Normalized;
        var wet = 1.0 + (conditions.Track.Wetness * 2.0);
        var firstLap = lap == 1 ? balance.FirstLapIncidentMultiplier : 1.0;
        var chance = balance.DriverErrorBaseRate * proneness * wet * firstLap;

        if (car.IncidentRng.NextDouble() >= chance)
        {
            return 0.0;
        }

        var severity = car.IncidentRng.NextDouble();
        var crashShare = DriverErrorCrashShare * (1.2 - attr.Racecraft.Normalized);
        if (severity < crashShare)
        {
            car.Running = false;
            car.Status = FinishStatus.Retired;
            car.RetirementReason = "Driver error";
            events.Add(new RaceEvent
            {
                Kind = RaceEventKind.DriverError, Lap = lap, CompetitorId = car.Id,
                Description = "Crashed out",
            });
            return 0.0;
        }

        events.Add(new RaceEvent
        {
            Kind = RaceEventKind.DriverError, Lap = lap, CompetitorId = car.Id,
            Description = "Off-track moment",
        });
        return balance.DriverErrorTimeLossSeconds * (0.3 + severity);
    }

    /// <summary>Roll for contact with the car ahead. Heavy contact ends this car's race and
    /// delays the other; lighter contact costs both some time. Poor racecraft and lap one raise
    /// the odds. The other party is recorded for the relationship layer (ADR-0013); full
    /// multi-car pile-ups come with the neutralisation model.</summary>
    private static double ApplyCollision(
        CarRaceState car, CarRaceState? ahead, int lap, BalanceCoefficients balance, List<RaceEvent> events)
    {
        var attr = car.Competitor.Driver.Attributes;
        var proneness = 0.5 + (1.0 - attr.Racecraft.Normalized);
        var firstLap = lap == 1 ? balance.FirstLapIncidentMultiplier : 1.0;
        var chance = balance.CollisionBaseRate * proneness * firstLap;

        if (car.IncidentRng.NextDouble() >= chance)
        {
            return 0.0;
        }

        var otherId = ahead?.Id;
        var severity = car.IncidentRng.NextDouble();
        if (severity < CollisionHeavyShare)
        {
            car.Running = false;
            car.Status = FinishStatus.Retired;
            car.RetirementReason = "Collision";
            if (ahead is { Running: true })
            {
                ahead.TotalTime += balance.CollisionTimeLossSeconds;
            }

            events.Add(new RaceEvent
            {
                Kind = RaceEventKind.Collision, Lap = lap, CompetitorId = car.Id,
                OtherCompetitorId = otherId, Description = "Collision",
            });
            return 0.0;
        }

        if (ahead is { Running: true })
        {
            ahead.TotalTime += balance.CollisionTimeLossSeconds * 0.4;
        }

        events.Add(new RaceEvent
        {
            Kind = RaceEventKind.Collision, Lap = lap, CompetitorId = car.Id,
            OtherCompetitorId = otherId, Description = "Contact",
        });
        return balance.CollisionTimeLossSeconds * (0.3 + severity);
    }

    // ---- Neutralisation (M5d) ---------------------------------------------

    /// <summary>Each car stopped on track this lap gets a chance (scaled by the circuit's
    /// safety-car likelihood) to bring out a neutralisation to recover it.</summary>
    private static bool TryTriggerNeutralization(
        IRandom rng, Circuit circuit, BalanceCoefficients balance, int retiredThisLap)
    {
        var chance = balance.SafetyCarFromIncidentChance * circuit.SafetyCarLikelihood.Normalized;
        var triggered = false;
        for (var k = 0; k < retiredThisLap; k++)
        {
            if (rng.NextDouble() < chance)
            {
                triggered = true;
            }
        }

        return triggered;
    }

    private static NeutralizationState ChooseNeutralization(IRandom rng, BalanceCoefficients balance)
    {
        if (rng.NextDouble() < balance.VirtualSafetyCarShare)
        {
            return NeutralizationState.VirtualSafetyCar;
        }

        return rng.NextDouble() < balance.RedFlagShare
            ? NeutralizationState.RedFlag
            : NeutralizationState.SafetyCar;
    }

    private static double NeutralizedLapTime(Circuit circuit, BalanceCoefficients balance) =>
        circuit.BaseLapTimeSeconds * balance.NeutralizationPaceFactor;

    /// <summary>Resume racing. A VSC kept the gaps, so nothing changes. A safety car bunches the
    /// field nose to tail (lapped cars unlap); a red flag does the same and grants fresh tyres.</summary>
    private static void ApplyRestart(
        List<CarRaceState> cars, NeutralizationState state, BalanceCoefficients balance, TyreCompound startingCompound)
    {
        if (state == NeutralizationState.VirtualSafetyCar)
        {
            return;
        }

        var running = cars
            .Where(c => c.Running)
            .OrderByDescending(c => c.LapsCompleted)
            .ThenBy(c => c.TotalTime)
            .ToList();
        if (running.Count == 0)
        {
            return;
        }

        var leaderLaps = running[0].LapsCompleted;
        var leaderTime = running[0].TotalTime;
        for (var i = 0; i < running.Count; i++)
        {
            var c = running[i];
            c.LapsCompleted = leaderLaps;
            c.TotalTime = leaderTime + (balance.BunchGapSeconds * i);
            if (state == NeutralizationState.RedFlag)
            {
                c.Tyre = TyreState.Fresh(startingCompound);
            }
        }
    }

    private static RaceEventKind ToEventKind(NeutralizationState state) => state switch
    {
        NeutralizationState.VirtualSafetyCar => RaceEventKind.VirtualSafetyCar,
        NeutralizationState.RedFlag => RaceEventKind.RedFlag,
        _ => RaceEventKind.SafetyCar,
    };

    private static string NeutralizationLabel(NeutralizationState state) => state switch
    {
        NeutralizationState.VirtualSafetyCar => "Virtual safety car",
        NeutralizationState.RedFlag => "Red flag",
        _ => "Safety car",
    };

    // ---- Ordering / classification ---------------------------------------

    /// <summary>For each running car, the car currently directly ahead of it — used to pair up
    /// collisions. Computed once per lap from the standing at the lap's start.</summary>
    private static Dictionary<string, CarRaceState> AheadMap(List<CarRaceState> cars)
    {
        var order = cars
            .Where(c => c.Running)
            .OrderByDescending(c => c.LapsCompleted)
            .ThenBy(c => c.TotalTime)
            .ToList();

        var map = new Dictionary<string, CarRaceState>(order.Count);
        for (var i = 1; i < order.Count; i++)
        {
            map[order[i].Id] = order[i - 1];
        }

        return map;
    }

    private static LapSnapshot SnapshotOf(int lap, List<CarRaceState> cars, NeutralizationState state)
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

        return new LapSnapshot { Lap = lap, Order = order, State = state };
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
