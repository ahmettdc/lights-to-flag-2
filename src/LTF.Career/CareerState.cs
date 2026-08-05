using LTF.Domain;
using LTF.Domain.Common;
using LTF.Domain.Management;
using LTF.Domain.Racing;

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

/// <summary>One team's persistent history line in a save (M11e).</summary>
public sealed record TeamHistoryRecord
{
    public required string TeamId { get; init; }
    public int ChampionshipsWon { get; init; }
    public int RaceWins { get; init; }
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
    };

    /// <summary>Reapply this snapshot onto a carset, returning a carset resumed at this point in the
    /// career. Drivers and teams the snapshot doesn't mention keep their carset values; the
    /// relationship graph and contract list are restored wholesale.</summary>
    public Carset RestoreInto(Carset carset)
    {
        var records = Drivers.ToDictionary(r => r.DriverId, StringComparer.Ordinal);
        var histories = Teams.ToDictionary(r => r.TeamId, StringComparer.Ordinal);

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
            .Select(t => histories.TryGetValue(t.Id, out var h)
                ? t with { ChampionshipsWon = h.ChampionshipsWon, RaceWins = h.RaceWins }
                : t)
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
}
