namespace LightsToFlag.Core.Simulation;

/// <summary>The four tyre families a competitor can run.</summary>
public enum TyreCompound
{
    Soft,
    Hard,
    Intermediate,
    Wet,
}

public static class TyreCompoundExtensions
{
    /// <summary>True for the wet-weather compounds (intermediate / full wet).</summary>
    public static bool IsWetWeather(this TyreCompound compound) =>
        compound is TyreCompound.Intermediate or TyreCompound.Wet;
}
