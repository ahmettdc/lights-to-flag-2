namespace LTF.Domain.Rnd;

/// <summary>
/// The twelve performance axes an R&amp;D node can target (M14 / ADR-0024). These are the forward-compatible
/// names of the eventual 12-axis car model; until that refinement lands (a separate M1/Faz-2 milestone),
/// <see cref="CarAxisMap"/> resolves each axis onto one of the five live <see cref="Racing.Car"/> ratings.
/// </summary>
public enum CarAxis
{
    AeroLowSpeed,
    AeroMediumSpeed,
    AeroHighSpeed,
    AeroFloor,
    DragEfficiency,
    MechanicalGrip,
    TyreManagement,
    PowerUnit,
    PowerUnitReliability,
    GearboxReliability,
    Braking,
    PitOperations,
}
