using LTF.Domain.Common;

namespace LTF.Simulation;

/// <summary>
/// The live condition of a set of tyres during a stint: which compound, how many laps
/// old, and how worn (0 fresh … 1 fully worn). Immutable; the race loop advances it each
/// lap via <see cref="TyreModel.Advance"/>.
/// </summary>
public sealed record TyreState
{
    public required TyreCompound Compound { get; init; }

    /// <summary>Laps completed on this set.</summary>
    public int Age { get; init; }

    /// <summary>Wear from 0 (fresh) to 1 (worn out).</summary>
    public double Wear { get; init; }

    /// <summary>A brand-new set of the given compound.</summary>
    public static TyreState Fresh(TyreCompound compound) => new() { Compound = compound };
}
