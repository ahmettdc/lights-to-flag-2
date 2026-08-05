using LTF.Domain.Management;

namespace LTF.Domain.Racing;

/// <summary>
/// Immutable team "card" and the aggregate that owns a team's racing and management
/// state: the car, its starting driver line-up, money, facilities, staff, sponsors and
/// component pool. The career layer evolves these; the carset provides the season-one values.
/// </summary>
public sealed record Team
{
    public required string Id { get; init; }
    public required string Name { get; init; }

    /// <summary>Short label for tight UI (e.g. timing tower), e.g. "TAL".</summary>
    public string ShortName { get; init; } = "";

    public string Base { get; init; } = "";
    public string Principal { get; init; } = "";
    public string Nationality { get; init; } = "";

    /// <summary>The class this team races in, for multi-class series (M9); empty = single class.</summary>
    public string Class { get; init; } = "";

    public required Car Car { get; init; }

    /// <summary>Driver ids filling this team's seats at the start of the career.</summary>
    public IReadOnlyList<string> DriverIds { get; init; } = [];

    public Finances Finances { get; init; } = new();
    public Facilities Facilities { get; init; } = Facilities.Default;

    public IReadOnlyList<Staff> Staff { get; init; } = [];
    public IReadOnlyList<Sponsor> Sponsors { get; init; } = [];
    public IReadOnlyList<PowerUnitComponent> Components { get; init; } = [];

    // --- History ---
    public int ChampionshipsWon { get; init; }
    public int RaceWins { get; init; }
}
