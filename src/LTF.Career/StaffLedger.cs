using System.Linq;
using LTF.Domain;

namespace LTF.Career;

/// <summary>
/// Moves staff between a team's roster and the free-agent pool (M14). Hiring pulls a free agent onto a
/// team (where their skill feeds <see cref="ResearchLedger"/>); releasing returns a team member to the
/// pool. Pure carset-in/carset-out, like <see cref="ContractLedger"/>; an unknown team or staff id is a
/// no-op, so callers can act on user input without pre-checking.
/// </summary>
public static class StaffLedger
{
    /// <summary>Hire a free agent onto a team.</summary>
    public static Carset Hire(Carset carset, string teamId, string staffId)
    {
        var member = carset.StaffPool.FirstOrDefault(s => s.Id == staffId);
        if (member is null || carset.Teams.All(t => t.Id != teamId))
        {
            return carset;
        }

        var pool = carset.StaffPool.Where(s => s.Id != staffId).ToList();
        var teams = carset.Teams
            .Select(t => t.Id == teamId ? t with { Staff = [.. t.Staff, member] } : t)
            .ToList();
        return carset with { StaffPool = pool, Teams = teams };
    }

    /// <summary>Release a team member back to the free-agent pool.</summary>
    public static Carset Release(Carset carset, string teamId, string staffId)
    {
        var team = carset.Teams.FirstOrDefault(t => t.Id == teamId);
        var member = team?.Staff.FirstOrDefault(s => s.Id == staffId);
        if (member is null)
        {
            return carset;
        }

        var teams = carset.Teams
            .Select(t => t.Id == teamId ? t with { Staff = t.Staff.Where(s => s.Id != staffId).ToList() } : t)
            .ToList();
        return carset with { Teams = teams, StaffPool = [.. carset.StaffPool, member] };
    }
}
