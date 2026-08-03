using LightsToFlag.Core.Domain;

namespace LightsToFlag.Core.Simulation;

/// <summary>
/// Prices a single lap in seconds from a <see cref="LapContext"/>. The model is a
/// fresh, transparent design (not a port of the original formulas): a class base
/// laptime scaled by a car+driver performance shortfall, plus additive penalties
/// for fuel weight, tyre wear, ballast and wet running. All parts are pure and
/// individually testable; <see cref="WithNoise"/> layers seeded lap-to-lap variance
/// on top of the deterministic core.
/// </summary>
public static class LapTimeCalculator
{
    // How much a full 0..10 performance shortfall stretches the lap (fraction of base).
    private const double PerformanceSpread = 0.06;

    // Weight of the car vs the driver in combined performance.
    private const double CarWeight = 0.65;
    private const double DriverWeight = 0.35;

    private const double FuelEffectScale = 0.5;
    private const double SetupBonusSeconds = 0.6;
    private const double TyreBaseLossSeconds = 1.5;
    private const double TyreCliffStartPercent = 80.0;
    private const double TyreCliffSeconds = 4.0;
    private const double WrongTyreForWetSeconds = 12.0;
    private const double WetBaseSeconds = 8.0;
    private const double NoiseScaleSeconds = 0.9;

    /// <summary>Deterministic lap time in seconds (no lap-to-lap noise).</summary>
    public static double Deterministic(in LapContext ctx)
    {
        var baseLap = BaseLap(ctx.Circuit, ctx.Competitor.ClassIndex);
        var performance = CombinedPerformance(ctx);

        // Better performance ⇒ smaller shortfall ⇒ faster lap.
        var shortfall = (10.0 - performance) / 10.0;
        var lap = baseLap * (1.0 + shortfall * PerformanceSpread);

        lap -= ctx.Competitor.SetupQuality * SetupBonusSeconds * Positive(ctx.Coefficients.SetupEffectiveness, 1.0);
        lap += FuelPenalty(ctx);
        lap += TyrePenalty(ctx);
        lap += BallastPenalty(ctx);
        lap += WetPenalty(ctx);

        return Math.Max(baseLap * 0.9, lap);
    }

    /// <summary>Deterministic lap time plus seeded Gaussian variance, tighter for consistent drivers.</summary>
    public static double WithNoise(in LapContext ctx, IRandom rng)
    {
        var lap = Deterministic(ctx);
        var consistency = Clamp01(ctx.Competitor.Driver.Consistency / 10.0);
        var spread = NoiseScaleSeconds * (1.0 - 0.8 * consistency);
        return lap + rng.NextGaussian() * spread;
    }

    /// <summary>Combined 0..10 car+driver performance for a lap on this circuit.</summary>
    public static double CombinedPerformance(in LapContext ctx)
    {
        var car = CarPerformance(ctx.Competitor.Car, ctx.Circuit, ctx.Coefficients);
        var driver = DriverPerformance(ctx.Competitor.Driver, ctx.Circuit, ctx.Coefficients, ctx.Wetness, ctx.QualifyingTrim);
        return CarWeight * car + DriverWeight * driver;
    }

    /// <summary>Car strength (0..10) weighted by the circuit's aero/mech/engine demands.</summary>
    public static double CarPerformance(TeamRating car, CircuitSpec circuit, Coefficients coeff)
    {
        var wAero = Positive(coeff.AeroImportance, 1.0) * (circuit.HighSpeedCorners + 1);
        var wMech = Positive(coeff.MechanicalGripImportance, 1.0) * (circuit.LowSpeedCorners + 1);
        var wEng = Positive(coeff.EngineImportance, 1.0) * (circuit.TopSpeeds + 1);
        var total = wAero + wMech + wEng;
        if (total <= 0)
        {
            return Rating(car.Aerodynamics);
        }

        return (Rating(car.Aerodynamics) * wAero
              + Rating(car.MechanicalGrip) * wMech
              + Rating(car.Engine) * wEng) / total;
    }

    /// <summary>Driver strength (0..10); wet running blends toward wet-weather skill.</summary>
    public static double DriverPerformance(
        DriverRating driver, CircuitSpec circuit, Coefficients coeff, double wetness, bool qualifying)
    {
        var dry = qualifying ? Rating(driver.Qualifying) : Rating(driver.Pace);
        var wet = Rating(driver.WetWeather);
        var blended = Lerp(dry, wet, Clamp01(wetness));

        // Higher difficulty circuits reward skill a touch more; kept gentle.
        var difficulty = Rating(circuit.Difficulty) / 10.0;
        var paceEffect = Positive(coeff.DriverPaceEffect, 1.0);
        var centred = 5.0 + (blended - 5.0) * paceEffect * (0.9 + 0.2 * difficulty);
        return Math.Clamp(centred, 0.0, 10.0);
    }

    private static double BaseLap(CircuitSpec circuit, int classIndex)
    {
        if (circuit.BaseLaptimeByClass.Count == 0)
        {
            return circuit.LapRecord > 0 ? circuit.LapRecord : 90.0;
        }

        var index = Math.Clamp(classIndex, 0, circuit.BaseLaptimeByClass.Count - 1);
        return circuit.BaseLaptimeByClass[index];
    }

    private static double FuelPenalty(in LapContext ctx)
    {
        var fuel = Math.Max(0.0, ctx.FuelLapsRemaining);
        return ctx.Circuit.FuelPenaltyPerLap * fuel * FuelEffectScale;
    }

    private static double TyrePenalty(in LapContext ctx)
    {
        var wear = Math.Clamp(ctx.TyreWearPercent, 0.0, 100.0);
        var wearFactor = wear / 100.0;
        var loss = wearFactor * (TyreBaseLossSeconds + 0.2 * Rating(ctx.Circuit.TyreWear));

        if (wear > TyreCliffStartPercent)
        {
            var cliff = (wear - TyreCliffStartPercent) / (100.0 - TyreCliffStartPercent);
            loss += cliff * TyreCliffSeconds;
        }

        return loss;
    }

    private static double BallastPenalty(in LapContext ctx) =>
        ctx.Circuit.BallastPenaltyPerKgPerLap * ctx.Competitor.BallastKg;

    private static double WetPenalty(in LapContext ctx)
    {
        var wetness = Clamp01(ctx.Wetness);
        if (wetness <= 0)
        {
            return ctx.Tyre.IsWetWeather() ? 1.0 : 0.0; // wet tyres on a dry track are slow
        }

        var penalty = WetBaseSeconds * wetness;
        if (!ctx.Tyre.IsWetWeather())
        {
            // Slicks in the rain: badly off the pace, eased slightly by driver skill.
            penalty += WrongTyreForWetSeconds * wetness * (1.0 - 0.4 * Rating(ctx.Competitor.Driver.WetWeather) / 10.0);
        }

        return penalty;
    }

    private static double Rating(int oneToTen) => Math.Clamp(oneToTen, 1, 10);

    private static double Positive(double value, double fallback) => value > 0 ? value : fallback;

    private static double Clamp01(double v) => Math.Clamp(v, 0.0, 1.0);

    private static double Lerp(double a, double b, double t) => a + (b - a) * t;
}
