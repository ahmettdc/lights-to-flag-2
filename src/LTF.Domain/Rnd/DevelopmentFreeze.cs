namespace LTF.Domain.Rnd;

/// <summary>
/// How hard a regulation freezes development of a car axis (Ri3 / ADR-0010). <see cref="InSeasonOnly"/> stops
/// the axis developing <em>during</em> a season — the FIA's in-season homologation freeze — but it may still be
/// worked on between seasons (the "winter"); <see cref="Full"/> stops it developing at all while the freeze is
/// in force. An axis absent from a regulation's freeze list is unrestricted (develops freely, in season and
/// out). FIA-wide: a freeze applies to every team, not just the player.
/// </summary>
public enum DevelopmentFreezeMode
{
    /// <summary>Frozen mid-season; still develops between seasons (the winter pulse).</summary>
    InSeasonOnly,

    /// <summary>Frozen entirely while the regulation is in force — no development, in season or out.</summary>
    Full,
}

/// <summary>
/// One car axis a regulation freezes, and how hard (Ri3). Carried on <see cref="Racing.RegulationSet"/>; read
/// by <see cref="ResearchState"/>'s development engine to gate which nodes may start or advance. Deterministic,
/// value-typed (so it round-trips a save byte-stably) — no RNG.
/// </summary>
public sealed record AxisFreeze
{
    /// <summary>The car axis this freeze restricts.</summary>
    public required CarAxis Axis { get; init; }

    /// <summary>How hard the axis is frozen.</summary>
    public required DevelopmentFreezeMode Mode { get; init; }
}
