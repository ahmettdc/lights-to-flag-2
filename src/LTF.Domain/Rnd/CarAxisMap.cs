namespace LTF.Domain.Rnd;

/// <summary>Which of the five live car ratings an axis develops (M14). <see cref="None"/> means the axis
/// has no car-rating home yet — e.g. pit operations feed the M7 pit crew, not a <see cref="Racing.Car"/>
/// rating.</summary>
public enum CarRatingTarget
{
    None,
    Aerodynamics,
    Chassis,
    PowerUnit,
    TyreGentleness,
    Reliability,
}

/// <summary>
/// Resolves an R&amp;D <see cref="CarAxis"/> onto the five-rating <see cref="Racing.Car"/> until the 12-axis
/// refinement lands (M1/Faz-2). Pure and total, so a node's category always has a defined effect target.
/// </summary>
public static class CarAxisMap
{
    public static CarRatingTarget TargetOf(CarAxis axis) => axis switch
    {
        CarAxis.AeroLowSpeed or CarAxis.AeroMediumSpeed or CarAxis.AeroHighSpeed
            or CarAxis.AeroFloor or CarAxis.DragEfficiency => CarRatingTarget.Aerodynamics,
        CarAxis.MechanicalGrip or CarAxis.Braking => CarRatingTarget.Chassis,
        CarAxis.PowerUnit => CarRatingTarget.PowerUnit,
        CarAxis.TyreManagement => CarRatingTarget.TyreGentleness,
        CarAxis.PowerUnitReliability or CarAxis.GearboxReliability => CarRatingTarget.Reliability,
        _ => CarRatingTarget.None, // PitOperations — no live Car rating yet
    };
}
