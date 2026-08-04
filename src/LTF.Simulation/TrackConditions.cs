namespace LTF.Simulation;

/// <summary>
/// The state of the track surface. <see cref="Wetness"/> runs 0 (bone dry) to 1 (standing
/// water); it evolves over a session via <see cref="WeatherModel.Evolve"/>.
/// </summary>
public readonly record struct TrackConditions(double Wetness)
{
    public static TrackConditions Dry => new(0.0);
}
