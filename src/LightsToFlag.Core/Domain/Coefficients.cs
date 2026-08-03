namespace LightsToFlag.Core.Domain;

/// <summary>
/// Global simulation tuning coefficients (see <c>Coefficients.txt</c>,
/// schema in <c>Carsetmaker/coefficientsdesc.txt</c> — 27 values).
/// Field order is preserved from the schema so a carset round-trips.
/// </summary>
public sealed record Coefficients
{
    public double PoorWeatherLikelihood { get; init; }
    public double WeatherChangeRate { get; init; }
    public double TrackGripChangeRate { get; init; }
    public double DriverPaceEffect { get; init; }
    public double DriverConsistencyEffect { get; init; }
    public double AeroImportance { get; init; }
    public double MechanicalGripImportance { get; init; }
    public double EngineImportance { get; init; }
    public double TyreWear { get; init; }
    public double ChassisWear { get; init; }
    public double EngineWear { get; init; }
    public double DriverErrorRate { get; init; }
    public double MechanicalProblemRate { get; init; }
    public double FuelWeightPenalty { get; init; }
    public double CollisionRate { get; init; }
    public double OvertakingRate { get; init; }
    public double SoftHardGap { get; init; }
    public double SoftHardWearRatio { get; init; }
    public double InSeasonDriverUpgradeRate { get; init; }
    public double InSeasonCarUpgradeRate { get; init; }
    public double SafetyCarPeriodFactor { get; init; }
    public double RefuellingTime { get; init; }
    public double TyreChangeTime { get; init; }
    public double DamageFixTime { get; init; }
    public double BlockingCoefficient { get; init; }
    public double SetupEffectiveness { get; init; }
    public double SafetyCarLikelihood { get; init; }
}
