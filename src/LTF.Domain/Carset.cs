using LTF.Domain.Management;
using LTF.Domain.Racing;
using LTF.Domain.Rnd;

namespace LTF.Domain;

/// <summary>
/// Aggregate root: everything a playable championship is made of. This is the immutable,
/// engine-facing value the content loader (M2) produces from a carset folder and that the
/// simulation and career layers read. Referenced entities are linked by id.
/// </summary>
public sealed record Carset
{
    /// <summary>Stable identifier (the carset folder name, e.g. "global-prix").</summary>
    public required string Id { get; init; }

    /// <summary>Display name, e.g. "Global Prix Series".</summary>
    public required string Name { get; init; }

    /// <summary>Content schema version, bumped when the on-disk format changes.</summary>
    public int SchemaVersion { get; init; } = 1;

    public required RulesSet Rules { get; init; }
    public BalanceCoefficients Balance { get; init; } = new();

    /// <summary>The technical regulation era this championship races under (ADR-0018); defaults to
    /// the DRS era so carsets without a <c>regulations</c> block behave exactly as before.</summary>
    public RegulationSet Regulations { get; init; } = RegulationSet.Drs;

    public required IReadOnlyList<Team> Teams { get; init; }

    /// <summary>Full race drivers filling the teams' seats.</summary>
    public required IReadOnlyList<Driver> Drivers { get; init; }

    public required IReadOnlyList<Circuit> Circuits { get; init; }

    /// <summary>The season schedule: circuits raced in order, on their dates (ADR-0011).</summary>
    public required IReadOnlyList<CalendarRound> Calendar { get; init; }

    /// <summary>Tyre range the series brings (soft → wet).</summary>
    public IReadOnlyList<TyreSpec> Tyres { get; init; } = [];

    /// <summary>Reserve / rookie pool promoted into seats at season rollover.</summary>
    public IReadOnlyList<Driver> Reserves { get; init; } = [];

    /// <summary>Free-agent staff available to hire.</summary>
    public IReadOnlyList<Staff> StaffPool { get; init; } = [];

    /// <summary>Sponsors available to sign.</summary>
    public IReadOnlyList<Sponsor> SponsorPool { get; init; } = [];

    /// <summary>The paddock's web of relationships (ADR-0013); empty until a career evolves them.</summary>
    public RelationshipGraph Relationships { get; init; } = RelationshipGraph.Empty;

    /// <summary>Active contracts binding drivers/staff to teams (M12); empty if the carset ships none.</summary>
    public IReadOnlyList<Contract> Contracts { get; init; } = [];

    /// <summary>The series-wide R&amp;D development catalog (M14 / ADR-0016); empty if the carset ships none.
    /// Per-team development progress lives on each team, not here.</summary>
    public TechTree TechTree { get; init; } = TechTree.Empty;
}
