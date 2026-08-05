using LTF.Domain;
using LTF.Domain.Common;
using LTF.Domain.Management;
using LTF.Domain.Racing;
using LTF.Domain.Rnd;

namespace LTF.Career;

/// <summary>One driver's persistent career line in a save (M11e/M12): the accumulated record plus the
/// dynamic morale and reputation that a career moves.</summary>
public sealed record DriverCareerRecord
{
    public required string DriverId { get; init; }
    public required DriverCareer Career { get; init; }
    public int Morale { get; init; } = 50;
    public int Reputation { get; init; } = 50;
}

/// <summary>One team's persistent history line in a save (M11e), plus its finances (M13): the balance
/// and cost cap that the economy moves between seasons.</summary>
public sealed record TeamHistoryRecord
{
    public required string TeamId { get; init; }
    public int ChampionshipsWon { get; init; }
    public int RaceWins { get; init; }
    public Finances Finances { get; init; } = new();
}

/// <summary>One relationship in a save (M12), stored with a plain integer affinity so the save format
/// stays simple and round-trips exactly (the domain <see cref="Affinity"/> is rebuilt on restore).</summary>
public sealed record RelationshipRecord
{
    public required string AId { get; init; }
    public required string BId { get; init; }
    public int Affinity { get; init; }
    public RelationshipKind Kind { get; init; }
}

/// <summary>One team's persistent R&amp;D progress in a save (M14): the car ratings and facility levels that
/// development has moved, the concept lean and regulation readiness, and the unlocked and in-flight nodes.
/// Car and facility values are plain ints, rebuilt (clamped) on restore — the same trick as morale and
/// affinity.</summary>
public sealed record TeamResearchRecord
{
    public required string TeamId { get; init; }

    // Evolved car ratings.
    public int Aerodynamics { get; init; }
    public int Chassis { get; init; }
    public int PowerUnit { get; init; }
    public int TyreGentleness { get; init; }
    public int Reliability { get; init; }

    // Facility levels.
    public int DesignOffice { get; init; }
    public int WindTunnel { get; init; }
    public int Cfd { get; init; }
    public int CompositeManufacturing { get; init; }
    public int MechanicalWorkshop { get; init; }
    public int QualityControl { get; init; }
    public int Simulator { get; init; }
    public int Dyno { get; init; }
    public int PitCrewCentre { get; init; }
    public int DataCentre { get; init; }

    public int RegulationReadiness { get; init; }
    public int AeroLean { get; init; }
    public int PowertrainLean { get; init; }
    public IReadOnlyList<string> UnlockedNodeIds { get; init; } = [];
    public IReadOnlyList<DevelopmentProject> ActiveProjects { get; init; } = [];
}

/// <summary>
/// A snapshot of a career's mutable progress (M11e/M12): the seed it plays from, the game date it has
/// reached, every driver's and team's record, the paddock's relationships, and the active contracts.
/// The static carset content — teams, drivers' talent, circuits, rules — is not stored: a save captures
/// only what changes as a career is played, and is reapplied onto a freshly loaded carset with
/// <see cref="RestoreInto"/>. Pure and I/O-free, as the Career layer must be; the Persistence layer
/// turns it into a file.
/// </summary>
public sealed record CareerState
{
    public required int Seed { get; init; }
    public required DateOnly Date { get; init; }
    public IReadOnlyList<DriverCareerRecord> Drivers { get; init; } = [];
    public IReadOnlyList<TeamHistoryRecord> Teams { get; init; } = [];
    public IReadOnlyList<RelationshipRecord> Relationships { get; init; } = [];
    public IReadOnlyList<Contract> Contracts { get; init; } = [];
    public IReadOnlyList<TeamResearchRecord> Research { get; init; } = [];

    /// <summary>Snapshot a carset's mutable progress at a given game date and seed.</summary>
    public static CareerState Capture(Carset carset, DateOnly date, int seed) => new()
    {
        Seed = seed,
        Date = date,
        Drivers = carset.Drivers
            .Select(d => new DriverCareerRecord
            {
                DriverId = d.Id,
                Career = d.Career,
                Morale = d.Morale.Value,
                Reputation = d.Reputation.Value,
            })
            .ToList(),
        Teams = carset.Teams
            .Select(t => new TeamHistoryRecord
            {
                TeamId = t.Id,
                ChampionshipsWon = t.ChampionshipsWon,
                RaceWins = t.RaceWins,
                Finances = t.Finances,
            })
            .ToList(),
        Relationships = carset.Relationships.Relationships
            .Select(r => new RelationshipRecord
            {
                AId = r.AId,
                BId = r.BId,
                Affinity = r.Affinity.Value,
                Kind = r.Kind,
            })
            .ToList(),
        Contracts = carset.Contracts,
        // R&D progress is only worth storing once a series actually runs a tech tree.
        Research = carset.TechTree.Nodes.Count == 0
            ? []
            : carset.Teams.Select(CaptureResearch).ToList(),
    };

    private static TeamResearchRecord CaptureResearch(Team t) => new()
    {
        TeamId = t.Id,
        Aerodynamics = t.Car.Aerodynamics.Value,
        Chassis = t.Car.Chassis.Value,
        PowerUnit = t.Car.PowerUnit.Value,
        TyreGentleness = t.Car.TyreGentleness.Value,
        Reliability = t.Car.Reliability.Value,
        DesignOffice = t.Facilities.DesignOffice.Value,
        WindTunnel = t.Facilities.WindTunnel.Value,
        Cfd = t.Facilities.Cfd.Value,
        CompositeManufacturing = t.Facilities.CompositeManufacturing.Value,
        MechanicalWorkshop = t.Facilities.MechanicalWorkshop.Value,
        QualityControl = t.Facilities.QualityControl.Value,
        Simulator = t.Facilities.Simulator.Value,
        Dyno = t.Facilities.Dyno.Value,
        PitCrewCentre = t.Facilities.PitCrewCentre.Value,
        DataCentre = t.Facilities.DataCentre.Value,
        RegulationReadiness = t.Research.RegulationReadiness,
        AeroLean = t.Research.Concept.AeroLean,
        PowertrainLean = t.Research.Concept.PowertrainLean,
        UnlockedNodeIds = t.Research.UnlockedNodeIds,
        ActiveProjects = t.Research.ActiveProjects,
    };

    /// <summary>Reapply this snapshot onto a carset, returning a carset resumed at this point in the
    /// career. Drivers and teams the snapshot doesn't mention keep their carset values; the
    /// relationship graph and contract list are restored wholesale.</summary>
    public Carset RestoreInto(Carset carset)
    {
        var records = Drivers.ToDictionary(r => r.DriverId, StringComparer.Ordinal);
        var histories = Teams.ToDictionary(r => r.TeamId, StringComparer.Ordinal);
        var research = Research.ToDictionary(r => r.TeamId, StringComparer.Ordinal);

        var drivers = carset.Drivers
            .Select(d => records.TryGetValue(d.Id, out var rec)
                ? d with
                {
                    Career = rec.Career,
                    Morale = Rating.Clamped(rec.Morale),
                    Reputation = Rating.Clamped(rec.Reputation),
                }
                : d)
            .ToList();
        var teams = carset.Teams
            .Select(t =>
            {
                var updated = histories.TryGetValue(t.Id, out var h)
                    ? t with { ChampionshipsWon = h.ChampionshipsWon, RaceWins = h.RaceWins, Finances = h.Finances }
                    : t;
                return research.TryGetValue(t.Id, out var r) ? RestoreResearch(updated, r) : updated;
            })
            .ToList();

        var graph = new RelationshipGraph
        {
            Relationships = Relationships
                .Select(r => new Relationship
                {
                    AId = r.AId,
                    BId = r.BId,
                    Affinity = Affinity.Clamped(r.Affinity),
                    Kind = r.Kind,
                })
                .ToList(),
        };

        return carset with { Drivers = drivers, Teams = teams, Relationships = graph, Contracts = Contracts };
    }

    private static Team RestoreResearch(Team team, TeamResearchRecord r) => team with
    {
        Car = team.Car with
        {
            Aerodynamics = Rating.Clamped(r.Aerodynamics),
            Chassis = Rating.Clamped(r.Chassis),
            PowerUnit = Rating.Clamped(r.PowerUnit),
            TyreGentleness = Rating.Clamped(r.TyreGentleness),
            Reliability = Rating.Clamped(r.Reliability),
        },
        Facilities = new Facilities
        {
            DesignOffice = FacilityLevel.Clamped(r.DesignOffice),
            WindTunnel = FacilityLevel.Clamped(r.WindTunnel),
            Cfd = FacilityLevel.Clamped(r.Cfd),
            CompositeManufacturing = FacilityLevel.Clamped(r.CompositeManufacturing),
            MechanicalWorkshop = FacilityLevel.Clamped(r.MechanicalWorkshop),
            QualityControl = FacilityLevel.Clamped(r.QualityControl),
            Simulator = FacilityLevel.Clamped(r.Simulator),
            Dyno = FacilityLevel.Clamped(r.Dyno),
            PitCrewCentre = FacilityLevel.Clamped(r.PitCrewCentre),
            DataCentre = FacilityLevel.Clamped(r.DataCentre),
        },
        Research = new ResearchState
        {
            UnlockedNodeIds = r.UnlockedNodeIds,
            ActiveProjects = r.ActiveProjects,
            Concept = new ConceptDirection { AeroLean = r.AeroLean, PowertrainLean = r.PowertrainLean },
            RegulationReadiness = r.RegulationReadiness,
        },
    };
}
