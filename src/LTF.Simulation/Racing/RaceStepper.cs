using LTF.Domain.Common;
using LTF.Domain.Racing;
using LTF.Simulation.Laps;
using LTF.Simulation.Practice;

namespace LTF.Simulation.Racing;

public static partial class RaceSimulator
{
    /// <summary>
    /// The race run as a resumable state machine (M23g): the same lights-out setup and per-lap body that
    /// <see cref="Run"/> used, hoisted out of a single method so a caller can advance the race one lap at a
    /// time — the seam a live, player-controllable race needs (M23i). <see cref="Run"/> now drives this stepper
    /// to the flag in one go, so there is a <em>single</em> implementation of the lap loop and the golden digest
    /// is preserved by construction (a lap-by-lap run and a one-shot <see cref="Run"/> execute the identical
    /// statements in the identical order, drawing the per-car streams identically). A parity test locks the two
    /// together. Nested in <see cref="RaceSimulator"/> so it keeps access to the private per-lap helpers.
    /// </summary>
    public sealed class RaceStepper
    {
        private readonly Circuit _circuit;
        private readonly RulesSet _rules;
        private readonly BalanceCoefficients _balance;
        private readonly RegulationSet _regs;
        private readonly bool _era2026;
        private readonly RaceFormat _fmt;
        private readonly TyreCompound _startingCompound;
        private readonly IRandom _raceControlRng;
        private readonly IRandom _trafficRng;
        private readonly List<CarRaceState> _cars;
        private readonly List<LapSnapshot> _snapshots;
        private readonly List<RaceEvent> _events;
        private NeutralizationState _state = NeutralizationState.Green;
        private int _neutralLapsLeft;
        private int _lap;

        public RaceStepper(
            Circuit circuit, IReadOnlyList<Competitor> grid, RulesSet rules, BalanceCoefficients balance,
            int seed, TyreCompound startingCompound = TyreCompound.Medium, RegulationSet? regulations = null,
            IReadOnlyDictionary<string, PracticeSetup>? setups = null, RaceFormat? format = null,
            IReadOnlyDictionary<string, TyreCompound>? startingCompounds = null)
        {
            _circuit = circuit;
            _rules = rules;
            _balance = balance;

            // Null regulations reproduce the DRS era exactly, so existing callers are unaffected.
            _regs = regulations ?? RegulationSet.Drs;
            _era2026 = _regs.Era == RegulationEra.ActiveAero2026;

            // A null format is a standard feature race, so existing callers run identically (M9).
            _fmt = format ?? RaceFormat.Standard;
            _startingCompound = startingCompound;

            var baseRng = new DeterministicRandom(seed);
            _raceControlRng = baseRng.Fork(RaceControlSalt);
            _trafficRng = baseRng.Fork(TrafficSalt);
            Laps = ResolveLaps(circuit, _fmt);
            var baseTargets = PlanPitLaps(Laps, rules.MandatoryPitStops);

            _cars = new List<CarRaceState>(grid.Count);
            for (var i = 0; i < grid.Count; i++)
            {
                var paceRng = baseRng.Fork(i + 1);
                var car = new CarRaceState(
                    grid[i], gridPosition: i + 1, rng: paceRng,
                    reliabilityRng: paceRng.Fork(ReliabilitySalt), incidentRng: paceRng.Fork(IncidentSalt),
                    pitRng: paceRng.Fork(PitSalt),
                    // A per-car starting compound (M23b, player strategy) overrides the field-wide default; with
                    // no per-car dictionary (every existing caller) this is exactly startingCompound, so the race
                    // — and the golden digest — is bit-for-bit unchanged.
                    tyre: TyreState.Fresh(startingCompounds?.GetValueOrDefault(grid[i].Id, startingCompound) ?? startingCompound),
                    fuel: 1.0,
                    health: ComponentHealth.Fresh(), mode: EngineMode.Standard,
                    topSpeed: TopSpeedFor(grid[i].Car, circuit, _regs, _era2026));
                car.PitPlan = StaggeredPlan(baseTargets, i, grid.Count, Laps, balance);
                car.Setup = setups?.GetValueOrDefault(grid[i].Id) ?? PracticeSetup.None;
                car.Ballast = _fmt.Ballast.GetValueOrDefault(grid[i].Id);
                _cars.Add(car);
            }

            _snapshots = new List<LapSnapshot>(Laps);
            _events = new List<RaceEvent>();

            // Lights out: start performance and any getaway trouble, before lap one.
            foreach (var car in _cars)
            {
                ApplyStart(car, balance, _fmt.StartType, _events);
            }
        }

        /// <summary>The race length in laps.</summary>
        public int Laps { get; }

        /// <summary>Laps completed so far (0 before the first <see cref="AdvanceLap"/>).</summary>
        public int CurrentLap => _lap;

        /// <summary>True once every lap has run — <see cref="AdvanceLap"/> is then a no-op.</summary>
        public bool IsComplete => _lap >= Laps;

        /// <summary>The running order at the end of the lap just advanced (the live telemetry the screen reads).</summary>
        public LapSnapshot LatestLap => _snapshots[^1];

        /// <summary>Advance the race by one lap. A no-op once <see cref="IsComplete"/>. The body is the verbatim
        /// per-lap logic the monolithic <see cref="Run"/> loop used, so the draw sequence is unchanged.</summary>
        public void AdvanceLap()
        {
            if (_lap >= Laps)
            {
                return;
            }

            var lap = ++_lap;

            // Aliases so the lap body below reads identically to the original monolithic loop; the cross-lap
            // neutralisation state is loaded from the fields and written back at the end.
            var circuit = _circuit;
            var rules = _rules;
            var balance = _balance;
            var regs = _regs;
            var era2026 = _era2026;
            var startingCompound = _startingCompound;
            var raceControlRng = _raceControlRng;
            var trafficRng = _trafficRng;
            var laps = Laps;
            var cars = _cars;
            var snapshots = _snapshots;
            var events = _events;
            var state = _state;
            var neutralLapsLeft = _neutralLapsLeft;

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
                        ApplyPitStop(
                            car, lap, startingCompound, balance, events,
                            neutralized: true, refuellingAllowed: rules.RefuellingAllowed);
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
                // R38: a damaged car runs reduced effective aero; inert (the original car) until a
                // carset opts in via DamageAeroLoss, so the pace draw stays identical by default.
                var effectiveCar = EffectiveCar(car, balance);
                var sectors = LapTimeModel.Simulate(
                    circuit, effectiveCar, car.Competitor.Driver.Attributes, balance, conditions, car.Rng);
                var lapTime = sectors.Total
                              + EngineModes.PaceDelta(car.Mode, balance)
                              + LimpPenalty(car.Health, balance)
                              + ComponentWearPenalty(car.Health, balance);

                // 2026 only: active aero gains time, a depleted battery de-rates the car — both
                // deterministic, no random draw.
                if (era2026)
                {
                    lapTime += ActiveAeroDelta(effectiveCar, circuit, regs);
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

                car.Tyre = TyreModel.Advance(
                    car.Tyre, circuit, car.Competitor.Driver.Attributes, car.Competitor.Car.TyreGentleness, balance);
                car.Fuel = FuelModel.Burn(car.Fuel, laps);

                // 2026 only: harvest energy back over the lap (Manual Override spends it in traffic).
                if (era2026)
                {
                    car.Energy = Math.Min(1.0, car.Energy + regs.EnergyRegenPerLap);
                }

                // Pit stop (M7a/M7b): once the car reaches its planned stop lap, fresh tyres cost time.
                if (car.PitStops < car.PitPlan.Count && lap >= car.PitPlan[car.PitStops])
                {
                    ApplyPitStop(
                        car, lap, startingCompound, balance, events,
                        neutralized: false, refuellingAllowed: rules.RefuellingAllowed);
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
                    ApplyRestart(cars, stateThisLap, circuit, balance, startingCompound, rules.DriversUnlapUnderSafetyCar);
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

            _state = state;
            _neutralLapsLeft = neutralLapsLeft;
        }

        /// <summary>Classify the race once every lap has run.</summary>
        public RaceResult Finish() => Classify(_cars, _rules, _snapshots, _events, _fmt);
    }
}
