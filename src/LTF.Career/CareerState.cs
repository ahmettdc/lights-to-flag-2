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

/// <summary>One team's seat roster in a save (M18): the driver ids it fields. Kept off
/// <see cref="TeamHistoryRecord"/> (which is compared by value) and captured only when the series develops
/// drivers, in which case the transfer market may have moved seats and this is authoritative.</summary>
public sealed record TeamRosterRecord
{
    public required string TeamId { get; init; }
    public IReadOnlyList<string> DriverIds { get; init; } = [];
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

/// <summary>One board member in a save (M17), stored with plain-int priorities and confidence so the save
/// round-trips exactly (the domain <see cref="Rating"/> and <see cref="Pressure"/> are rebuilt on restore).</summary>
public sealed record BoardMemberRecord
{
    public required string Id { get; init; }
    public string Name { get; init; } = "";
    public int SportingPriority { get; init; }
    public int FinancialPriority { get; init; }
    public int LongTermPriority { get; init; }
    public int BrandPriority { get; init; }
    public int DriverDevPriority { get; init; }
    public int ConfidenceInPlayer { get; init; }
    public int RiskTolerance { get; init; }
    public BoardMemberTraits Traits { get; init; }
}

/// <summary>One team's board in a save (M17): its ownership, the six pressures and firing risk a career
/// moves (as plain ints), its members, and its objectives. Objectives carry no value structs, so they are
/// stored as the domain type directly.</summary>
public sealed record TeamBoardRecord
{
    public required string TeamId { get; init; }
    public OwnershipType Ownership { get; init; }
    public int BoardConfidence { get; init; }
    public int SportingPressure { get; init; }
    public int FinancialPressure { get; init; }
    public int SponsorPressure { get; init; }
    public int MediaPressure { get; init; }
    public int InternalPressure { get; init; }
    public int FiringRisk { get; init; }
    public IReadOnlyList<BoardMemberRecord> Members { get; init; } = [];
    public IReadOnlyList<Objective> Objectives { get; init; } = [];
}

/// <summary>
/// A full snapshot of one driver in a save (M18): everything needed to rebuild the driver — the age and
/// attributes a development career moves, the hidden potential, and the regen drivers a carset never
/// shipped. Value structs are flattened to plain ints and rebuilt (clamped) on restore, the same trick as
/// the other records; <see cref="DriverCareer"/> and personality ride along so a resumed driver is whole.
/// </summary>
public sealed record DriverRosterRecord
{
    public required string Id { get; init; }
    public string FirstName { get; init; } = "";
    public string LastName { get; init; } = "";
    public int Age { get; init; }
    public string Nationality { get; init; } = "";
    public int Number { get; init; }

    // Attributes.
    public int Pace { get; init; }
    public int Racecraft { get; init; }
    public int Consistency { get; init; }
    public int TyreManagement { get; init; }
    public int WetWeather { get; init; }
    public int Feedback { get; init; }

    public int Potential { get; init; }
    public int Morale { get; init; }
    public int Reputation { get; init; }
    public DriverCareer Career { get; init; } = DriverCareer.None;

    // Personality.
    public int Ego { get; init; }
    public int Loyalty { get; init; }
    public int Temperament { get; init; }
    public int Ambition { get; init; }
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

    /// <summary>The team the player runs (M17); empty for an all-AI career.</summary>
    public string PlayerTeamId { get; init; } = "";

    /// <summary>Each team's board state (M17); empty for a career with no boards.</summary>
    public IReadOnlyList<TeamBoardRecord> Boards { get; init; } = [];

    /// <summary>Every driver's full snapshot (M18); empty unless the series develops drivers, in which case
    /// it is authoritative — it carries evolved ages/attributes and the regen drivers the carset never
    /// shipped, which the id-keyed <see cref="Drivers"/> deltas alone would drop on restore.</summary>
    public IReadOnlyList<DriverRosterRecord> Roster { get; init; } = [];

    /// <summary>The reserve/rookie pool's full snapshot (M18); empty unless the series develops drivers.</summary>
    public IReadOnlyList<DriverRosterRecord> ReservePool { get; init; } = [];

    /// <summary>Each team's seat roster (M18); empty unless the series develops drivers, in which case the
    /// transfer market may have moved seats and this is authoritative.</summary>
    public IReadOnlyList<TeamRosterRecord> TeamRosters { get; init; } = [];

    /// <summary>Snapshot a carset's mutable progress at a given game date and seed.</summary>
    public static CareerState Capture(Carset carset, DateOnly date, int seed)
    {
        // A developing series moves ages, attributes and the roster, and grows drivers the carset never
        // shipped — so a full dynamic snapshot is captured (and made authoritative on restore). A series
        // that develops no one keeps the lighter id-keyed deltas, so its save is byte-identical to before.
        var develops = carset.Rules.DriverDevelopment.IsActive;

        return new CareerState
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
            // R&D progress (and the car ratings a regulation change moves) is worth storing once a series
            // runs a tech tree or develops — otherwise it stays inert.
            Research = carset.TechTree.Nodes.Count == 0 && !develops
                ? []
                : carset.Teams.Select(CaptureResearch).ToList(),
            PlayerTeamId = carset.PlayerTeamId,
            // Board state is only worth storing once a series runs boards (byte-identity gate).
            Boards = carset.Boards.Count == 0 ? [] : carset.Boards.Select(CaptureBoard).ToList(),
            Roster = develops ? carset.Drivers.Select(CaptureRoster).ToList() : [],
            ReservePool = develops ? carset.Reserves.Select(CaptureRoster).ToList() : [],
            TeamRosters = develops
                ? carset.Teams.Select(t => new TeamRosterRecord { TeamId = t.Id, DriverIds = t.DriverIds }).ToList()
                : [],
        };
    }

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

    private static TeamBoardRecord CaptureBoard(TeamBoard b) => new()
    {
        TeamId = b.TeamId,
        Ownership = b.Ownership,
        BoardConfidence = b.Metrics.BoardConfidence.Value,
        SportingPressure = b.Metrics.SportingPressure.Value,
        FinancialPressure = b.Metrics.FinancialPressure.Value,
        SponsorPressure = b.Metrics.SponsorPressure.Value,
        MediaPressure = b.Metrics.MediaPressure.Value,
        InternalPressure = b.Metrics.InternalPressure.Value,
        FiringRisk = b.FiringRisk.Value,
        Members = b.Members.Select(CaptureMember).ToList(),
        Objectives = b.Objectives,
    };

    private static BoardMemberRecord CaptureMember(BoardMember m) => new()
    {
        Id = m.Id,
        Name = m.Name,
        SportingPriority = m.SportingPriority.Value,
        FinancialPriority = m.FinancialPriority.Value,
        LongTermPriority = m.LongTermPriority.Value,
        BrandPriority = m.BrandPriority.Value,
        DriverDevPriority = m.DriverDevPriority.Value,
        ConfidenceInPlayer = m.ConfidenceInPlayer.Value,
        RiskTolerance = m.RiskTolerance.Value,
        Traits = m.Traits,
    };

    /// <summary>Reapply this snapshot onto a carset, returning a carset resumed at this point in the
    /// career. Drivers and teams the snapshot doesn't mention keep their carset values; the
    /// relationship graph and contract list are restored wholesale.</summary>
    public Carset RestoreInto(Carset carset)
    {
        var records = Drivers.ToDictionary(r => r.DriverId, StringComparer.Ordinal);
        var histories = Teams.ToDictionary(r => r.TeamId, StringComparer.Ordinal);
        var research = Research.ToDictionary(r => r.TeamId, StringComparer.Ordinal);
        var rosters = TeamRosters.ToDictionary(r => r.TeamId, StringComparer.Ordinal);

        // A developing career captured its full roster — evolved ages/attributes and the regen drivers the
        // carset never shipped — so that snapshot is authoritative; otherwise the id-keyed deltas overlay
        // the carset's own drivers (unmentioned drivers keep their carset values).
        var evolved = Roster.Count > 0;
        var drivers = evolved
            ? Roster.Select(RestoreRosterDriver).ToList()
            : carset.Drivers
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
                if (rosters.TryGetValue(t.Id, out var roster) && roster.DriverIds.Count > 0)
                {
                    updated = updated with { DriverIds = roster.DriverIds };
                }

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

        var boards = Boards.Count == 0 ? carset.Boards : Boards.Select(RestoreBoard).ToList();
        var reserves = evolved ? ReservePool.Select(RestoreRosterDriver).ToList() : carset.Reserves;

        return carset with
        {
            Drivers = drivers,
            Teams = teams,
            Relationships = graph,
            Contracts = Contracts,
            PlayerTeamId = PlayerTeamId,
            Boards = boards,
            Reserves = reserves,
        };
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

    private static TeamBoard RestoreBoard(TeamBoardRecord r) => new()
    {
        TeamId = r.TeamId,
        Ownership = r.Ownership,
        Metrics = new PressureMetrics
        {
            BoardConfidence = Pressure.Clamped(r.BoardConfidence),
            SportingPressure = Pressure.Clamped(r.SportingPressure),
            FinancialPressure = Pressure.Clamped(r.FinancialPressure),
            SponsorPressure = Pressure.Clamped(r.SponsorPressure),
            MediaPressure = Pressure.Clamped(r.MediaPressure),
            InternalPressure = Pressure.Clamped(r.InternalPressure),
        },
        FiringRisk = Pressure.Clamped(r.FiringRisk),
        Members = r.Members.Select(RestoreMember).ToList(),
        Objectives = r.Objectives,
    };

    private static BoardMember RestoreMember(BoardMemberRecord r) => new()
    {
        Id = r.Id,
        Name = r.Name,
        SportingPriority = Rating.Clamped(r.SportingPriority),
        FinancialPriority = Rating.Clamped(r.FinancialPriority),
        LongTermPriority = Rating.Clamped(r.LongTermPriority),
        BrandPriority = Rating.Clamped(r.BrandPriority),
        DriverDevPriority = Rating.Clamped(r.DriverDevPriority),
        ConfidenceInPlayer = Pressure.Clamped(r.ConfidenceInPlayer),
        RiskTolerance = Rating.Clamped(r.RiskTolerance),
        Traits = r.Traits,
    };

    private static DriverRosterRecord CaptureRoster(Driver d) => new()
    {
        Id = d.Id,
        FirstName = d.FirstName,
        LastName = d.LastName,
        Age = d.Age,
        Nationality = d.Nationality,
        Number = d.Number,
        Pace = d.Attributes.Pace.Value,
        Racecraft = d.Attributes.Racecraft.Value,
        Consistency = d.Attributes.Consistency.Value,
        TyreManagement = d.Attributes.TyreManagement.Value,
        WetWeather = d.Attributes.WetWeather.Value,
        Feedback = d.Attributes.Feedback.Value,
        Potential = d.Potential,
        Morale = d.Morale.Value,
        Reputation = d.Reputation.Value,
        Career = d.Career,
        Ego = d.Personality.Ego.Value,
        Loyalty = d.Personality.Loyalty.Value,
        Temperament = d.Personality.Temperament.Value,
        Ambition = d.Personality.Ambition.Value,
    };

    private static Driver RestoreRosterDriver(DriverRosterRecord r) => new()
    {
        Id = r.Id,
        FirstName = r.FirstName,
        LastName = r.LastName,
        Age = r.Age,
        Nationality = r.Nationality,
        Number = r.Number,
        Attributes = new DriverAttributes
        {
            Pace = Rating.Clamped(r.Pace),
            Racecraft = Rating.Clamped(r.Racecraft),
            Consistency = Rating.Clamped(r.Consistency),
            TyreManagement = Rating.Clamped(r.TyreManagement),
            WetWeather = Rating.Clamped(r.WetWeather),
            Feedback = Rating.Clamped(r.Feedback),
        },
        Potential = r.Potential,
        Morale = Rating.Clamped(r.Morale),
        Reputation = Rating.Clamped(r.Reputation),
        Career = r.Career,
        Personality = new Personality
        {
            Ego = Rating.Clamped(r.Ego),
            Loyalty = Rating.Clamped(r.Loyalty),
            Temperament = Rating.Clamped(r.Temperament),
            Ambition = Rating.Clamped(r.Ambition),
        },
    };
}
