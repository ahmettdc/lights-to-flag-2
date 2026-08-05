using LTF.Domain;
using LTF.Domain.Racing;

namespace LTF.Career;

/// <summary>One driver's persistent career line in a save (M11e).</summary>
public sealed record DriverCareerRecord
{
    public required string DriverId { get; init; }
    public required DriverCareer Career { get; init; }
}

/// <summary>One team's persistent history line in a save (M11e).</summary>
public sealed record TeamHistoryRecord
{
    public required string TeamId { get; init; }
    public int ChampionshipsWon { get; init; }
    public int RaceWins { get; init; }
}

/// <summary>
/// A snapshot of a career's mutable progress (M11e): the seed it plays from, the game date it has
/// reached, and every driver's and team's accumulated record. The static carset content — teams,
/// drivers' talent, circuits, rules — is not stored: a save captures only what changes as a career
/// is played, and is reapplied onto a freshly loaded carset with <see cref="RestoreInto"/>. Pure and
/// I/O-free, as the Career layer must be; the Persistence layer turns it into a file.
/// </summary>
public sealed record CareerState
{
    public required int Seed { get; init; }
    public required DateOnly Date { get; init; }
    public IReadOnlyList<DriverCareerRecord> Drivers { get; init; } = [];
    public IReadOnlyList<TeamHistoryRecord> Teams { get; init; } = [];

    /// <summary>Snapshot a carset's mutable progress at a given game date and seed.</summary>
    public static CareerState Capture(Carset carset, DateOnly date, int seed) => new()
    {
        Seed = seed,
        Date = date,
        Drivers = carset.Drivers
            .Select(d => new DriverCareerRecord { DriverId = d.Id, Career = d.Career })
            .ToList(),
        Teams = carset.Teams
            .Select(t => new TeamHistoryRecord
            {
                TeamId = t.Id,
                ChampionshipsWon = t.ChampionshipsWon,
                RaceWins = t.RaceWins,
            })
            .ToList(),
    };

    /// <summary>Reapply this snapshot's records onto a carset, returning a carset resumed at this
    /// point in the career. Drivers and teams the snapshot doesn't mention keep their carset values.</summary>
    public Carset RestoreInto(Carset carset)
    {
        var careers = Drivers.ToDictionary(r => r.DriverId, r => r.Career, StringComparer.Ordinal);
        var histories = Teams.ToDictionary(r => r.TeamId, StringComparer.Ordinal);

        var drivers = carset.Drivers
            .Select(d => careers.TryGetValue(d.Id, out var career) ? d with { Career = career } : d)
            .ToList();
        var teams = carset.Teams
            .Select(t => histories.TryGetValue(t.Id, out var h)
                ? t with { ChampionshipsWon = h.ChampionshipsWon, RaceWins = h.RaceWins }
                : t)
            .ToList();

        return carset with { Drivers = drivers, Teams = teams };
    }
}
