using System.Collections.Generic;
using LTF.Career;
using LTF.Domain;
using LTF.Domain.Management;
using LTF.Domain.Racing;

namespace LTF.App.Session;

/// <summary>
/// Assembles a brand-new career from the player's choices, App-side (no engine change). Mirrors the
/// Team-Principal setup <c>BossCareerSweep</c> uses: designate the player's team, apply the board
/// objectives agreed during setup, seed any remaining boards with their ownership defaults, and open a
/// session. Deterministic; takes no difficulty argument, so no UI setting can reach the engine.
/// </summary>
public static class NewCareerService
{
    /// <summary>
    /// Build a session for <paramref name="teamId"/> in <paramref name="carset"/>, applying the
    /// <paramref name="objectives"/> agreed with the board (empty ⇒ the board keeps its authored/default
    /// objectives). The seed defaults to <see cref="SessionLoader.DefaultSeed"/> for M19 parity.
    /// </summary>
    public static ShellSession Create(
        Carset carset,
        string teamId,
        IReadOnlyList<Objective> objectives,
        int seed = SessionLoader.DefaultSeed)
    {
        var withTeam = carset with { PlayerTeamId = teamId };
        var withBoard = WithPlayerObjectives(withTeam, teamId, objectives);
        var seeded = BoardReview.SetObjectives(withBoard);
        return ShellSession.FromCarset(seeded, seed);
    }

    // Put the agreed objectives on the player's board, synthesising a neutral board if the carset ships none
    // for that team so board pressure is meaningful. No agreed objectives ⇒ leave the boards untouched
    // (BoardReview.SetObjectives still fills any empty existing board from its ownership default).
    private static Carset WithPlayerObjectives(Carset carset, string teamId, IReadOnlyList<Objective> objectives)
    {
        if (objectives.Count == 0)
        {
            return carset;
        }

        var boards = new List<TeamBoard>(carset.Boards);
        var index = boards.FindIndex(b => string.CompareOrdinal(b.TeamId, teamId) == 0);
        if (index >= 0)
        {
            boards[index] = boards[index] with { Objectives = objectives };
        }
        else
        {
            boards.Add(new TeamBoard { TeamId = teamId, Objectives = objectives });
        }

        return carset with { Boards = boards };
    }
}
