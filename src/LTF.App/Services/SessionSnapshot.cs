using System.Linq;
using LTF.App.Session;
using LTF.Domain.Racing;

namespace LTF.App.Services;

/// <summary>
/// Derives the top-bar values from a <see cref="ShellSession"/>: date from the clock, and the player
/// team's name/badge, cost-cap room and board confidence — each gated on the carset actually carrying it
/// (<see cref="LTF.Domain.Carset.PlayerTeam"/>, finances, boards).
/// </summary>
public sealed class SessionSnapshot : ISessionSnapshot
{
    public SessionSnapshot(ShellSession session)
    {
        HasSession = true;
        Date = session.Clock.Date;

        var team = session.Carset.PlayerTeam();
        if (team is null)
        {
            return;
        }

        HasPlayerTeam = true;
        TeamName = team.Name;
        TeamBadge = Badge(team);

        if (team.Finances.HasCostCap)
        {
            HasCap = true;
            CapRoom = team.Finances.CostCap;
        }

        var board = session.Carset.Boards
            .FirstOrDefault(b => string.CompareOrdinal(b.TeamId, team.Id) == 0);
        if (board is not null)
        {
            HasBoard = true;
            BoardConfidence = board.Metrics.BoardConfidence.Value;
        }
    }

    public bool HasSession { get; }

    public DateOnly Date { get; }

    public bool HasPlayerTeam { get; }

    public string TeamName { get; } = "";

    public string TeamBadge { get; } = "";

    public bool HasCap { get; }

    public long CapRoom { get; }

    public bool HasBoard { get; }

    public int BoardConfidence { get; }

    private static string Badge(Team team)
    {
        if (team.ShortName.Length > 0)
        {
            return team.ShortName.ToUpperInvariant();
        }

        var letters = team.Name.Where(char.IsLetter).Take(2).ToArray();
        return new string(letters).ToUpperInvariant();
    }
}
