using LightsToFlag.Core.Domain;

namespace LightsToFlag.Core.Simulation;

/// <summary>
/// Simulates a full race lap-by-lap from a starting grid to a classification.
/// It integrates each car's time through the race applying fuel burn, tyre wear
/// and pit stops, rolls reliability and driver-error retirements, folds in a
/// weather timeline, and can throw a single bunching safety-car period. The model
/// is a clean design (not a port) and is fully deterministic given the RNG.
/// </summary>
public sealed class RaceSimulator
{
    private const double GridGapSeconds = 0.3;
    private const double SafetyCarBunchGapSeconds = 1.0;
    private const double DriverErrorTimeLossSeconds = 8.0;
    private const double FuelMarginLaps = 1.0;

    public RaceClassification Run(
        IReadOnlyList<Competitor> competitors,
        QualifyingResult grid,
        CircuitSpec circuit,
        Coefficients coeff,
        RulesSet rules,
        IRandom rng)
    {
        var totalLaps = ResolveLaps(circuit);
        var byId = competitors.ToDictionary(c => c.Id);

        // Seed cars in grid order (unqualified competitors fall in at the back).
        var cars = new List<RaceCar>(competitors.Count);
        var gridOrder = grid.Grid
            .Where(g => byId.ContainsKey(g.CompetitorId))
            .Select(g => g.CompetitorId)
            .Concat(competitors.Select(c => c.Id).Where(id => grid.PositionOf(id) == 0))
            .ToList();

        for (var i = 0; i < gridOrder.Count; i++)
        {
            var competitor = byId[gridOrder[i]];
            cars.Add(new RaceCar(competitor, i, totalLaps, PlanStops(circuit, rules, totalLaps)));
        }

        var weather = WeatherModel.Generate(circuit, coeff, totalLaps, rng);
        var safetyCarLap = RollSafetyCarLap(circuit, coeff, totalLaps, rng);

        for (var lap = 1; lap <= totalLaps; lap++)
        {
            var wetness = weather[Math.Min(lap - 1, weather.Length - 1)];
            foreach (var car in cars.Where(c => c.Running))
            {
                AdvanceOneLap(car, circuit, coeff, wetness, rng);
            }

            if (lap == safetyCarLap)
            {
                ApplySafetyCarBunching(cars);
            }
        }

        return Classify(cars, totalLaps, rules);
    }

    private static void AdvanceOneLap(
        RaceCar car, CircuitSpec circuit, Coefficients coeff, double wetness, IRandom rng)
    {
        // Pit this lap?
        var lapTime = 0.0;
        if (car.StopLaps.Contains(car.Laps + 1))
        {
            lapTime += circuit.PitLaneSeconds + coeff.TyreChangeTime;
            car.Wear = 0;
            car.Tyre = NextCompound(car.Tyre);
            car.StopsDone++;
        }

        // Switch to wet rubber if it is clearly raining and we are on slicks (reactive stop).
        if (wetness > 0.5 && !car.Tyre.IsWetWeather() && !car.ForcedWetStopDone)
        {
            lapTime += circuit.PitLaneSeconds + coeff.TyreChangeTime;
            car.Wear = 0;
            car.Tyre = wetness > 0.75 ? TyreCompound.Wet : TyreCompound.Intermediate;
            car.ForcedWetStopDone = true;
        }

        var ctx = new LapContext
        {
            Competitor = car.Competitor,
            Circuit = circuit,
            Coefficients = coeff,
            FuelLapsRemaining = car.Fuel,
            TyreWearPercent = car.Wear,
            Tyre = car.Tyre,
            Wetness = wetness,
            QualifyingTrim = false,
        };

        var thisLap = LapTimeCalculator.WithNoise(ctx, rng);

        // Driver error: costs time, and occasionally ends the race.
        if (RollDriverError(car.Competitor, coeff, rng))
        {
            if (rng.NextDouble() < Math.Clamp(coeff.CollisionRate, 0.0, 1.0) * 0.4)
            {
                car.Retire(car.Laps, "Accident");
                return;
            }

            thisLap += DriverErrorTimeLossSeconds;
        }

        lapTime += thisLap;
        car.TotalTime += lapTime;
        car.Laps++;
        car.Fuel = Math.Max(0.0, car.Fuel - 1.0);
        car.Wear = Math.Min(100.0, car.Wear + TyreModel.WearPerLap(car.Competitor, circuit, car.Tyre));
        car.BestLap = Math.Min(car.BestLap, thisLap);

        // Mechanical reliability roll.
        if (RollMechanicalFailure(car.Competitor, circuit, coeff, rng))
        {
            car.Retire(car.Laps, "Mechanical");
        }
    }

    private static void ApplySafetyCarBunching(IReadOnlyList<RaceCar> cars)
    {
        var running = cars.Where(c => c.Running).OrderBy(c => c.TotalTime).ToList();
        if (running.Count == 0)
        {
            return;
        }

        var leader = running[0].TotalTime;
        for (var i = 0; i < running.Count; i++)
        {
            running[i].TotalTime = leader + i * SafetyCarBunchGapSeconds;
        }
    }

    private static RaceClassification Classify(IReadOnlyList<RaceCar> cars, int totalLaps, RulesSet rules)
    {
        // Finishers first (most laps, then least time), retirements after (by laps reached).
        var ordered = cars
            .OrderByDescending(c => c.Running)
            .ThenByDescending(c => c.Laps)
            .ThenBy(c => c.TotalTime)
            .ThenBy(c => c.Competitor.Id, StringComparer.Ordinal)
            .ToList();

        var fastest = cars
            .Where(c => c.BestLap < double.MaxValue)
            .OrderBy(c => c.BestLap)
            .ThenBy(c => c.Competitor.Id, StringComparer.Ordinal)
            .FirstOrDefault();

        var entries = new List<RaceEntryResult>(ordered.Count);
        for (var i = 0; i < ordered.Count; i++)
        {
            var car = ordered[i];
            var position = i + 1;
            var points = car.Running ? PointsForPosition(rules, position) : 0.0;

            var hasFastest = fastest is not null && ReferenceEquals(fastest, car);
            // Fastest-lap bonus only counts when classified within the points positions.
            if (hasFastest && car.Running && position <= rules.PrimaryPoints.Count)
            {
                points += rules.PointsForFastestLap;
            }

            entries.Add(new RaceEntryResult
            {
                Position = position,
                CompetitorId = car.Competitor.Id,
                Status = car.Running ? FinishStatus.Finished : FinishStatus.Retired,
                LapsCompleted = car.Laps,
                TotalTimeSeconds = car.TotalTime,
                Points = points,
                FastestLap = hasFastest,
                RetirementReason = car.RetirementReason,
            });
        }

        return new RaceClassification
        {
            Entries = entries,
            TotalLaps = totalLaps,
            FastestLapCompetitorId = fastest?.Competitor.Id,
        };
    }

    private static double PointsForPosition(RulesSet rules, int position)
    {
        var table = rules.PrimaryPoints;
        return position >= 1 && position <= table.Count ? table[position - 1] : 0.0;
    }

    private static bool RollMechanicalFailure(Competitor c, CircuitSpec circuit, Coefficients coeff, IRandom rng)
    {
        var unreliability = (10 - Math.Clamp(c.Car.Reliability, 1, 10)) / 10.0;
        var attrition = 0.5 + Math.Clamp(circuit.AttritionRate, 0, 10) / 10.0;
        var perLap = Math.Max(0.0, coeff.MechanicalProblemRate) * unreliability * attrition;
        return rng.NextDouble() < perLap;
    }

    private static bool RollDriverError(Competitor c, Coefficients coeff, IRandom rng)
    {
        var carelessness = (10 - Math.Clamp(c.Driver.Concentration, 1, 10)) / 10.0;
        var perLap = Math.Max(0.0, coeff.DriverErrorRate) * carelessness * 0.5;
        return rng.NextDouble() < perLap;
    }

    private static int RollSafetyCarLap(CircuitSpec circuit, Coefficients coeff, int totalLaps, IRandom rng)
    {
        var likelihood = Math.Clamp(circuit.SafetyCarLikelihood, 0, 10) / 10.0 * Math.Max(0.0, coeff.SafetyCarLikelihood);
        if (rng.NextDouble() >= Math.Clamp(likelihood, 0.0, 0.95) || totalLaps < 4)
        {
            return -1;
        }

        // Somewhere in the first two-thirds of the race.
        return 2 + rng.Next(Math.Max(1, totalLaps * 2 / 3));
    }

    private static IReadOnlyList<int> PlanStops(CircuitSpec circuit, RulesSet rules, int totalLaps)
    {
        var stops = Math.Max(rules.TyreChangesAllowed ? 1 : 0, Math.Max(0, circuit.MandatoryPitStops));
        if (stops <= 0 || totalLaps < 3)
        {
            return Array.Empty<int>();
        }

        var laps = new List<int>(stops);
        for (var i = 1; i <= stops; i++)
        {
            laps.Add((int)Math.Round((double)totalLaps * i / (stops + 1)));
        }

        return laps;
    }

    private static int ResolveLaps(CircuitSpec circuit)
    {
        if (circuit.Laps > 0)
        {
            return circuit.Laps;
        }

        var baseLap = circuit.BaseLaptimeByClass.Count > 0 ? circuit.BaseLaptimeByClass[0] : 90.0;
        var minutes = circuit.Minutes > 0 ? circuit.Minutes : 120;
        return Math.Max(1, (int)Math.Round(minutes * 60.0 / Math.Max(1.0, baseLap)));
    }

    private static TyreCompound NextCompound(TyreCompound current) =>
        current == TyreCompound.Soft ? TyreCompound.Hard : TyreCompound.Soft;

    /// <summary>Mutable per-car race state, private to the simulator.</summary>
    private sealed class RaceCar
    {
        public RaceCar(Competitor competitor, int gridIndex, int totalLaps, IReadOnlyList<int> stopLaps)
        {
            Competitor = competitor;
            Tyre = TyreCompound.Soft;
            Fuel = totalLaps + FuelMarginLaps;
            TotalTime = gridIndex * GridGapSeconds; // grid gives a small persistent head start
            StopLaps = new HashSet<int>(stopLaps);
        }

        public Competitor Competitor { get; }
        public double TotalTime { get; set; }
        public int Laps { get; set; }
        public double Wear { get; set; }
        public double Fuel { get; set; }
        public TyreCompound Tyre { get; set; }
        public bool Running { get; private set; } = true;
        public double BestLap { get; set; } = double.MaxValue;
        public HashSet<int> StopLaps { get; }
        public int StopsDone { get; set; }
        public bool ForcedWetStopDone { get; set; }
        public string? RetirementReason { get; private set; }

        public void Retire(int lap, string reason)
        {
            Running = false;
            Laps = lap;
            RetirementReason = reason;
        }
    }
}
