namespace LTF.Domain.Rnd;

/// <summary>
/// Where a development project sits in the pit-track validation pipeline (M14 / ADR-0024). A project
/// walks In Design → In Manufacture → Ready for Track Test → Fitted for Practice → Data Review, then
/// either is Approved for Race (its gain becomes permanent), sent back to Rework, or Abandoned. The car
/// rating changes only on <see cref="ApprovedForRace"/>.
/// </summary>
public enum ValidationState
{
    InDesign,
    InManufacture,
    ReadyForTrackTest,
    FittedForPractice,
    DataReview,
    ApprovedForRace,
    Rework,
    Abandoned,
}
