using LTF.Domain.Common;
using LTF.Domain.Racing;
using LTF.Simulation.Laps;
using LTF.Simulation.Practice;

namespace LTF.Simulation.Racing;

/// <summary>
/// Runs a race lap by lap and produces a classification, an event log and telemetry. Fully
/// deterministic: every car draws pace, reliability and incidents from separate streams
/// forked off the race seed, plus a race-control stream for neutralisations, so the same
/// seed and grid reproduce the same race bit for bit. M5a laid the pace skeleton, M5b added
/// reliability, M5c the start / driver errors / collisions, and M5d the safety car, VSC and
/// red-flag neutralisations. M6 added traffic and overtaking; 26a made the overtaking aid and
/// energy regulation-era-aware (DRS ↔ 2026 Manual Override) and 26b added the 2026 active-aero
/// lap-time gain and its top-speed effect. The energy and aero models draw no random numbers —
/// they are deterministic arithmetic — so the pre-2026 streams are untouched and a DRS-era race
/// stays bit-for-bit identical to before. M7a added pit stops on a fifth per-car RNG stream
/// (stop-time variance), so a botched stop never disturbs pace, reliability or incidents; M7b
/// staggered the stops per car and lets a car take a due stop cheaply under a neutralisation;
/// M7c added on-track penalties — track limits (deterministic strike counting) and a rare unsafe
/// pit release (drawn last on the pit stream), both gated so pre-M7c races stay bit-for-bit identical.
/// M8 accepts an optional practice setup per car that shaves a little off each green lap and cuts
/// the driver-error chance; with no setup (the default) the race is unchanged. M9 accepts an
/// optional <see cref="RaceFormat"/> bundle; M9a lets it set the race length (a timed duration or a
/// lap-count override), defaulting to the circuit's laps.
/// </summary>
public static class RaceSimulator
{
    private const long ReliabilitySalt = 0x5245_4C49; // "RELI"
    private const long IncidentSalt = 0x494E_4344;    // "INCD"
    private const long RaceControlSalt = 0x5241_4345; // "RACE"
    private const long TrafficSalt = 0x5452_4143;     // "TRAC"
    private const long PitSalt = 0x5049_5453;         // "PITS"

    // Below this health the weakest component starts costing lap time (limp mode).
    private const double LimpThreshold = 0.30;

    // How sharply failure risk climbs as a component wears out (added at zero health).
    private const double LowHealthRiskMultiplier = 5.0;

    // Share of driver errors / collisions severe enough to end a car's race.
    private const double DriverErrorCrashShare = 0.12;
    private const double CollisionHeavyShare = 0.20;

    // A rolling start's getaway loss is gentler than a standing start's (M9e).
    private const double RollingStartLossFactor = 0.3;

    // Representative top speed (kph): a base plus a power-unit-scaled span, weighted by track power.
    private const double BaseTopSpeedKph = 300.0;
    private const double TopSpeedSpanKph = 40.0;

    // Overtaking: pace advantage saturates at this many seconds/lap; the epsilon avoids
    // battling over pure lap-to-lap noise.
    private const double PaceAdvantageScale = 1.5;
    private const double PaceEpsilon = 0.05;

    private static readonly ComponentKind[] Components = Enum.GetValues<ComponentKind>();

    public static RaceResult Run(
        Circuit circuit, IReadOnlyList<Competitor> grid, RulesSet rules, BalanceCoefficients balance,
        int seed, TyreCompound startingCompound = TyreCompound.Medium, RegulationSet? regulations = null,
        IReadOnlyDictionary<string, PracticeSetup>? setups = null, RaceFormat? format = null)
    {
        // Null regulations reproduce the DRS era exactly, so existing callers are unaffected.
        var regs = regulations ?? RegulationSet.Drs;
        var era2026 = regs.Era == RegulationEra.ActiveAero2026;

        // A null format is a standard feature race, so existing callers run identically (M9).
        var fmt = format ?? RaceFormat.Standard;

        var baseRng = new DeterministicRandom(seed);
        var raceControlRng = baseRng.Fork(RaceControlSalt);
        var trafficRng = baseRng.Fork(TrafficSalt);
        var laps = ResolveLaps(circuit, fmt);
        var baseTargets = PlanPitLaps(laps, rules.MandatoryPitStops);

        var cars = new List<CarRaceState>(grid.Count);
        for (var i = 0; i < grid.Count; i++)
        {
            var paceRng = baseRng.Fork(i + 1);
            var car = new CarRaceState(
                grid[i], gridPosition: i + 1, rng: paceRng,
                reliabilityRng: paceRng.Fork(ReliabilitySalt), incidentRng: paceRng.Fork(IncidentSalt),
                pitRng: paceRng.Fork(PitSalt),
                tyre: TyreState.Fresh(startingCompound), fuel: 1.0,
                health: ComponentHealth.Fresh(), mode: EngineMode.Standard,
                topSpeed: TopSpeedFor(grid[i].Car, circuit, regs, era2026));
            car.PitPlan = StaggeredPlan(baseTargets, i, grid.Count, laps, balance);
            car.Setup = setups?.GetValueOrDefault(grid[i].Id) ?? PracticeSetup.None;
            car.Ballast = fmt.Ballast.GetValueOrDefault(grid[i].Id);
            cars.Add(car);
        }

        var snapshots = new List<LapSnapshot>(laps);
        var events = new List<RaceEvent>();

        // Lights out: start performance and any getaway trouble, before lap one.
        foreach (var car in cars)
        {
            ApplyStart(car, balance, fmt.StartType, events);
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
                    var neutralLap = NeutralizedLapTime(circuit, balance);
                    car.TotalTime += neutralLap;
                    car.LapsCompleted = lap;
                    car.LastLap = neutralLap;
                    car.LastSectors = EvenSectors(neutralLap);
                    car.Fuel = FuelModel.Burn(car.Fuel, laps);

                    // M7b: take a due stop now — a stop under a neutralisation is cheap.
                    if (car.PitStops < car.PitPlan.Count
                        && lap >= car.PitPlan[car.PitStops] - balance.NeutralizationPitWindowLaps)
                    {
                        ApplyPitStop(car, lap, startingCompound, balance, events, neutralized: true);
                    }

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
                var sectors = LapTimeModel.Simulate(circuit, car.Competitor, balance, conditions, car.Rng);
                var lapTime = sectors.Total
                              + EngineModes.PaceDelta(car.Mode, balance)
                              + LimpPenalty(car.Health, balance);

                // 2026 only: active aero gains time, a depleted battery de-rates the car — both
                // deterministic, no random draw.
                if (era2026)
                {
                    lapTime += ActiveAeroDelta(car.Competitor.Car, circuit, regs);
                    lapTime += DeRatingPenalty(car, regs);
                }

                // M8: a productive practice weekend shaves a little off every green lap. Zero
                // without practice, so a race with no practice setup is unchanged.
                lapTime -= car.Setup.RaceBonusSeconds;

                // M9c: success ballast adds lap time. Zero without ballast, so a race with none is
                // unchanged.
                lapTime += car.Ballast;

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
                car.LastLap = sectors.Total;
                car.LastSectors = sectors;
                if (lapTime < car.BestLap)
                {
                    car.BestLap = lapTime;
                }

                car.Tyre = TyreModel.Advance(car.Tyre, circuit, car.Competitor.Driver.Attributes, balance);
                car.Fuel = FuelModel.Burn(car.Fuel, laps);

                // 2026 only: harvest energy back over the lap (Manual Override spends it in traffic).
                if (era2026)
                {
                    car.Energy = Math.Min(1.0, car.Energy + regs.EnergyRegenPerLap);
                }

                // Pit stop (M7a/M7b): once the car reaches its planned stop lap, fresh tyres cost time.
                if (car.PitStops < car.PitPlan.Count && lap >= car.PitPlan[car.PitStops])
                {
                    ApplyPitStop(car, lap, startingCompound, balance, events, neutralized: false);
                }
            }

            // Traffic: cars in each other's wake battle for position (skipped under neutralisation).
            if (!neutralized)
            {
                ResolveTraffic(cars, circuit, balance, regs, era2026, trafficRng, lap, events);
            }

            // Decide the next state before snapshotting, so a restart's bunching is captured now.
            NeutralizationState nextState;
            int nextLeft;
            if (neutralized)
            {
                nextLeft = neutralLapsLeft - 1;
                if (nextLeft <= 0)
                {
                    ApplyRestart(cars, stateThisLap, balance, startingCompound, rules.DriversUnlapUnderSafetyCar);
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

            // Tally the lap leader for leading-lap points (M9b); deterministic, no random draw.
            var lapLeader = LeaderOf(cars);
            if (lapLeader is not null)
            {
                lapLeader.LapsLed++;
            }

            snapshots.Add(SnapshotOf(lap, cars, stateThisLap));
            state = nextState;
            neutralLapsLeft = nextLeft;
        }

        return Classify(cars, rules, snapshots, events, fmt);
    }

    /// <summary>The race length in laps (M9a): a lap-count override wins; else a target duration is
    /// converted with the circuit's representative lap time; else the circuit's own lap count. A
    /// standard format uses none of these, so it returns <c>circuit.Laps</c> exactly as before.</summary>
    private static int ResolveLaps(Circuit circuit, RaceFormat fmt)
    {
        if (fmt.LapOverride > 0)
        {
            return fmt.LapOverride;
        }

        if (fmt.TimedDurationSeconds > 0.0)
        {
            return Math.Max(1, (int)Math.Ceiling(fmt.TimedDurationSeconds / circuit.BaseLapTimeSeconds));
        }

        return circuit.Laps;
    }

    // ---- Start ------------------------------------------------------------

    /// <summary>Bake start performance into the car's time before lap one: a skill-based loss off an
    /// ideal getaway plus random spread, and a rare jump start that draws a penalty. A rolling start
    /// (M9e) softens the getaway loss and can't jump the start; both start types draw the same two
    /// numbers, so the incident stream is identical whichever is used.</summary>
    private static void ApplyStart(
        CarRaceState car, BalanceCoefficients balance, RaceStartType startType, List<RaceEvent> events)
    {
        var attr = car.Competitor.Driver.Attributes;
        var quality = (0.5 * attr.Racecraft.Normalized) + (0.5 * attr.Consistency.Normalized);
        var lossFactor = startType == RaceStartType.Rolling ? RollingStartLossFactor : 1.0;
        var loss = balance.StartSkillSeconds * (1.0 - quality) * lossFactor;
        var jitter = car.IncidentRng.NextGaussian() * balance.StartSpreadSeconds;
        car.TotalTime += Math.Max(0.0, loss + jitter);

        var jumped = car.IncidentRng.NextDouble() < balance.StartIncidentRate;
        if (jumped && startType == RaceStartType.Standing)
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

    // ---- Pit stops (M7a–M7c) ----------------------------------------------

    private static readonly TyreCompound[] DryRotation =
        [TyreCompound.Medium, TyreCompound.Hard, TyreCompound.Soft];

    /// <summary>The laps a car targets for its planned stops: the mandated number, spread evenly
    /// through the race. Empty when the rules mandate no stop. A car pits on the first green lap at
    /// or after a target, so a neutralised target lap doesn't make it miss the stop.</summary>
    private static IReadOnlyList<int> PlanPitLaps(int laps, int mandatoryStops)
    {
        if (mandatoryStops <= 0 || laps < 2)
        {
            return [];
        }

        var targets = new int[mandatoryStops];
        for (var k = 0; k < mandatoryStops; k++)
        {
            targets[k] = Math.Clamp((int)Math.Round((double)laps * (k + 1) / (mandatoryStops + 1)), 1, laps - 1);
        }

        return targets;
    }

    /// <summary>A car's staggered pit plan: the shared targets shifted by a small per-car offset so
    /// the field spreads its stops (undercut / overcut) instead of all pitting on the same lap.</summary>
    private static IReadOnlyList<int> StaggeredPlan(
        IReadOnlyList<int> baseTargets, int carIndex, int carCount, int laps, BalanceCoefficients balance)
    {
        if (baseTargets.Count == 0)
        {
            return [];
        }

        var offset = Math.Clamp(carIndex - (carCount / 2), -balance.PitStaggerLaps, balance.PitStaggerLaps);
        var plan = new int[baseTargets.Count];
        for (var k = 0; k < baseTargets.Count; k++)
        {
            plan[k] = Math.Clamp(baseTargets[k] + offset, 1, laps - 1);
        }

        return plan;
    }

    /// <summary>Change to a fresh set, rotating through the dry compounds so at least two distinct
    /// compounds are used across the race (the both-compounds rule). Adds pit-lane loss, a stationary
    /// time with spread and — rarely — a botched stop. Draws only from the car's own pit stream.</summary>
    private static void ApplyPitStop(
        CarRaceState car, int lap, TyreCompound startingCompound, BalanceCoefficients balance,
        List<RaceEvent> events, bool neutralized)
    {
        car.PitStops++;
        var compound = PitCompound(car.PitStops, startingCompound);
        car.Tyre = TyreState.Fresh(compound);

        // Under a neutralisation the field is slow, so the pit-lane loss is heavily discounted (M7b).
        var laneLoss = neutralized
            ? balance.PitLaneTimeLossSeconds * balance.NeutralizationPitDiscount
            : balance.PitLaneTimeLossSeconds;

        var loss = laneLoss
                   + balance.PitStopStationarySeconds
                   + Math.Abs(car.PitRng.NextGaussian() * balance.PitStopSpreadSeconds);

        var slow = car.PitRng.NextDouble() < balance.SlowPitStopChance;
        if (slow)
        {
            loss += balance.SlowPitStopExtraSeconds;
        }

        car.TotalTime += loss;
        events.Add(new RaceEvent
        {
            Kind = RaceEventKind.Pit,
            Lap = lap,
            CompetitorId = car.Id,
            Description = (slow ? "Slow pit stop → " : "Pit stop → ") + compound,
        });

        // Unsafe release (M7c): rarely the car is let go into an unsafe gap and takes a time
        // penalty. Drawn last — after the stationary jitter and the slow-stop check — so those
        // earlier draws keep their order and a first stop's timing is unchanged from M7a/M7b.
        if (car.PitRng.NextDouble() < balance.UnsafePitReleaseChance)
        {
            car.TotalTime += balance.UnsafePitReleasePenaltySeconds;
            events.Add(new RaceEvent
            {
                Kind = RaceEventKind.Penalty,
                Lap = lap,
                CompetitorId = car.Id,
                Description = "Unsafe pit release (time penalty)",
            });
        }
    }

    /// <summary>The fresh compound for a stop, rotated off the starting compound so the stint plan
    /// always uses at least two distinct dry compounds.</summary>
    private static TyreCompound PitCompound(int stopNumber, TyreCompound starting)
    {
        var startIndex = Array.IndexOf(DryRotation, starting);
        if (startIndex < 0)
        {
            startIndex = 0; // a wet / intermediate start rotates off the medium base
        }

        return DryRotation[(startIndex + stopNumber) % DryRotation.Length];
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

        // M8: race-simulation practice makes a driver less error-prone (factor ≤ 1); 1.0 (no
        // change) without practice, so the incident stream is untouched in a no-practice race.
        var chance = balance.DriverErrorBaseRate * proneness * wet * firstLap * car.Setup.ErrorFactor;

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

        var timeLoss = balance.DriverErrorTimeLossSeconds * (0.3 + severity);

        // Track limits (M7c): repeated off-track moments earn a time penalty once the allowance is
        // used up, then the count resets. Purely deterministic counting — no random draw — so the
        // incident stream is untouched and pre-M7c races stay bit-for-bit identical.
        car.TrackLimitStrikes++;
        if (car.TrackLimitStrikes > balance.TrackLimitAllowance)
        {
            car.TrackLimitStrikes = 0;
            timeLoss += balance.TrackLimitPenaltySeconds;
            events.Add(new RaceEvent
            {
                Kind = RaceEventKind.Penalty, Lap = lap, CompetitorId = car.Id,
                Description = "Track limits (time penalty)",
            });
        }

        return timeLoss;
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

    /// <summary>Representative top speed (kph): a base plus a power-unit-scaled span weighted by track
    /// power. In 2026 the low-drag (X) aero mode adds more top speed on power-sensitive circuits.</summary>
    private static double TopSpeedFor(Car car, Circuit circuit, RegulationSet regs, bool era2026)
    {
        var speed = BaseTopSpeedKph
                    + (car.PowerUnit.Normalized * TopSpeedSpanKph * (0.6 + (0.4 * circuit.PowerSensitivity.Normalized)));
        if (era2026)
        {
            speed += regs.LowDragTopSpeedKph * circuit.PowerSensitivity.Normalized;
        }

        return speed;
    }

    /// <summary>2026 active aero: a per-lap time gain (negative) blending the low-drag (X) benefit on
    /// the straights — scaled by the circuit's power sensitivity and the car's power unit — with the
    /// high-downforce (Z) benefit in the corners — scaled by downforce sensitivity and the car's aero.
    /// Automatic in 2026; deterministic, no random draw.</summary>
    private static double ActiveAeroDelta(Car car, Circuit circuit, RegulationSet regs)
    {
        var lowDrag = regs.LowDragLapGainSeconds * circuit.PowerSensitivity.Normalized
                      * (0.5 + (0.5 * car.PowerUnit.Normalized));
        var highDownforce = regs.HighDownforceLapGainSeconds * circuit.DownforceSensitivity.Normalized
                            * (0.5 + (0.5 * car.Aerodynamics.Normalized));
        return -(lowDrag + highDownforce);
    }

    private static SectorTimes EvenSectors(double total) =>
        new(total * 0.34, total * 0.33, total * 0.33);

    // ---- Traffic & overtaking (M6) ---------------------------------------

    /// <summary>Resolve dirty air and overtaking for the running order this lap. A car within
    /// combat range of the car ahead loses time in its wake; a genuinely faster car rolls to
    /// pass — success moves it ahead and logs the overtake, failure leaves it held up. The
    /// overtaking aid is era-aware: the DRS era uses the slipstream boost, while 2026 deploys a
    /// Manual Override drawn from the battery (no charge, no boost — and deploying spends energy
    /// whatever the outcome). The random draw is identical in both eras, so a DRS-era race is
    /// unchanged. Skipped under neutralisation and when traffic is disabled (combat threshold 0).</summary>
    private static void ResolveTraffic(
        List<CarRaceState> cars, Circuit circuit, BalanceCoefficients balance, RegulationSet regs, bool era2026,
        IRandom rng, int lap, List<RaceEvent> events)
    {
        if (balance.CombatThresholdSeconds <= 0.0)
        {
            return;
        }

        var order = cars
            .Where(c => c.Running)
            .OrderByDescending(c => c.LapsCompleted)
            .ThenBy(c => c.TotalTime)
            .ToList();

        for (var i = 1; i < order.Count; i++)
        {
            var follower = order[i];
            var leader = order[i - 1];
            if (follower.TotalTime - leader.TotalTime > balance.CombatThresholdSeconds)
            {
                continue;
            }

            // In the wake: anyone loses a little; a genuinely faster car also tries to pass.
            if (follower.LastLap >= leader.LastLap - PaceEpsilon)
            {
                follower.TotalTime += balance.DirtyAirLossSeconds * 0.5;
                continue;
            }

            // Overtaking aid: DRS slipstream (pre-2026) or a battery-limited Manual Override (2026).
            double boost;
            var description = "Overtake";
            if (era2026)
            {
                if (follower.Energy >= regs.ManualOverrideEnergyCost)
                {
                    follower.Energy -= regs.ManualOverrideEnergyCost;
                    boost = regs.ManualOverrideBoost;
                    description = "Overtake (override)";
                }
                else
                {
                    boost = 0.0; // battery too low to deploy the override — no boost this lap
                }
            }
            else
            {
                boost = balance.SlipstreamBoost;
            }

            if (rng.NextDouble() < OvertakeChance(follower, leader, circuit, balance, boost))
            {
                follower.TotalTime = leader.TotalTime - balance.PassMarginSeconds;
                events.Add(new RaceEvent
                {
                    Kind = RaceEventKind.Overtake, Lap = lap, CompetitorId = follower.Id,
                    OtherCompetitorId = leader.Id, Description = description,
                });
            }
            else
            {
                follower.TotalTime += balance.DirtyAirLossSeconds;
            }
        }
    }

    private static double OvertakeChance(
        CarRaceState follower, CarRaceState leader, Circuit circuit, BalanceCoefficients balance, double boost)
    {
        var paceAdvantage = Math.Clamp((leader.LastLap - follower.LastLap) / PaceAdvantageScale, 0.0, 1.0);
        var attack = 0.5 + follower.Competitor.Driver.Attributes.Racecraft.Normalized;
        var defence = 1.5 - leader.Competitor.Driver.Attributes.Racecraft.Normalized;
        var slipstream = 1.0 + boost;
        return balance.OvertakeBaseChance * circuit.Overtaking.Normalized * paceAdvantage * attack * defence * slipstream;
    }

    /// <summary>2026 de-rating: extra lap time from a depleted battery; 0 until energy drops below
    /// the threshold, then growing as it approaches empty. Mirrors <see cref="LimpPenalty"/>.</summary>
    private static double DeRatingPenalty(CarRaceState car, RegulationSet regs)
    {
        if (car.Energy >= regs.DeRatingThreshold)
        {
            return 0.0;
        }

        return regs.DeRatingPenaltySeconds * ((regs.DeRatingThreshold - car.Energy) / regs.DeRatingThreshold);
    }

    /// <summary>Resume racing. A VSC kept the gaps, so nothing changes. A safety car bunches the
    /// field nose to tail (lapped cars unlap); a red flag does the same and grants fresh tyres.</summary>
    private static void ApplyRestart(
        List<CarRaceState> cars, NeutralizationState state, BalanceCoefficients balance, TyreCompound startingCompound,
        bool driversUnlap)
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

            // Waved past to unlap (M9e): by default lapped cars join the lead lap; when the rule is
            // off they keep their own lap count and stay behind the lead-lap group.
            if (driversUnlap || c.LapsCompleted == leaderLaps)
            {
                c.LapsCompleted = leaderLaps;
            }

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

    /// <summary>The current race leader — most laps, then least time — or null if no car is running.
    /// Used to tally laps led (M9b); mirrors the ordering in <see cref="SnapshotOf"/>.</summary>
    private static CarRaceState? LeaderOf(List<CarRaceState> cars)
    {
        CarRaceState? leader = null;
        foreach (var c in cars)
        {
            if (!c.Running)
            {
                continue;
            }

            if (leader is null
                || c.LapsCompleted > leader.LapsCompleted
                || (c.LapsCompleted == leader.LapsCompleted && c.TotalTime < leader.TotalTime))
            {
                leader = c;
            }
        }

        return leader;
    }

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
        var order = new List<CarLapSample>(running.Count);
        for (var i = 0; i < running.Count; i++)
        {
            var c = running[i];
            var intervalAhead = i == 0 ? 0.0 : c.TotalTime - running[i - 1].TotalTime;
            order.Add(new CarLapSample
            {
                CompetitorId = c.Id,
                Position = i + 1,
                Laps = c.LapsCompleted,
                TotalTime = c.TotalTime,
                GapToLeader = c.TotalTime - leaderTime,
                IntervalAhead = intervalAhead,
                LastLap = c.LastLap,
                Sector1 = c.LastSectors.Sector1,
                Sector2 = c.LastSectors.Sector2,
                Sector3 = c.LastSectors.Sector3,
                TyreCompound = c.Tyre.Compound,
                TyreAge = c.Tyre.Age,
                TyreWear = c.Tyre.Wear,
                Fuel = c.Fuel,
                EngineMode = c.Mode,
                Energy = c.Energy,
            });
        }

        return new LapSnapshot { Lap = lap, Order = order, State = state };
    }

    private static RaceResult Classify(
        List<CarRaceState> cars, RulesSet rules, List<LapSnapshot> snapshots, List<RaceEvent> events, RaceFormat fmt)
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

        // The single car that led the most laps (M9b), for the most-laps-led point.
        CarRaceState? mostLed = null;
        foreach (var c in cars)
        {
            if (c.LapsLed > 0 && (mostLed is null || c.LapsLed > mostLed.LapsLed))
            {
                mostLed = c;
            }
        }

        var leaderTime = ordered.Count > 0 ? ordered[0].TotalTime : 0.0;
        var classCounts = new Dictionary<string, int>(StringComparer.Ordinal);
        var entries = new List<RaceClassificationEntry>(ordered.Count);
        for (var i = 0; i < ordered.Count; i++)
        {
            var c = ordered[i];
            var position = i + 1;

            // Dense position within the car's class (M9d); "" for a single-class field gives
            // ClassPosition == Position.
            var classId = c.Competitor.Class;
            classCounts.TryGetValue(classId, out var classCount);
            classCount++;
            classCounts[classId] = classCount;

            // M9f: a sprint scores from the sprint table, a feature from the race table.
            var points = c.Status == FinishStatus.Finished
                ? (fmt.IsSprint ? rules.Points.SprintPointsFor(position) : rules.Points.PointsFor(position))
                : 0;
            if (rules.Points.FastestLapPoint > 0 && fastest is not null && c.Id == fastest.Id
                && c.Status == FinishStatus.Finished && position <= 10)
            {
                points += rules.Points.FastestLapPoint;
            }

            // M9b: pole / leading-lap / most-laps-led points, all gated on a non-zero value (so a
            // default carset is unchanged) and awarded on the achievement itself, not on finishing.
            if (rules.Points.PolePoint > 0 && c.Id == fmt.PoleSitterId)
            {
                points += rules.Points.PolePoint;
            }

            if (rules.Points.LeadingLapPoint > 0 && c.LapsLed > 0)
            {
                points += rules.Points.LeadingLapPoint;
            }

            if (rules.Points.MostLapsLedPoint > 0 && mostLed is not null && c.Id == mostLed.Id)
            {
                points += rules.Points.MostLapsLedPoint;
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
                TopSpeed = c.TopSpeed,
                Points = points,
                ClassId = classId,
                ClassPosition = classCount,
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
