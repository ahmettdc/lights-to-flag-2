namespace LTF.App.Services;

/// <summary>
/// Read-only top-bar values derived from the current session. Each field is feature-gated (HasX) so the
/// shell shows only what the carset actually provides — mirroring the CLI's feature gating.
/// </summary>
public interface ISessionSnapshot
{
    bool HasSession { get; }

    /// <summary>The career's current game date (for display; formatted with the current culture).</summary>
    DateOnly Date { get; }

    bool HasPlayerTeam { get; }

    string TeamName { get; }

    /// <summary>Short badge label for the team tile (e.g. "KR").</summary>
    string TeamBadge { get; }

    bool HasCap { get; }

    /// <summary>Cost-cap room, in whole currency units.</summary>
    long CapRoom { get; }

    bool HasBoard { get; }

    /// <summary>Board confidence in the principal, 0–100.</summary>
    int BoardConfidence { get; }
}
