using LTF.App.Services;
using LTF.App.Session;
using Xunit;

namespace LTF.App.Tests;

/// <summary>
/// The composition root: the shell loads the real flagship carset and derives live top-bar values from
/// the player team; the snapshot feature-gates cleanly when a field is absent; loading is deterministic.
/// </summary>
public class SessionTests
{
    [Fact]
    public void Flagship_session_exposes_live_top_bar_values()
    {
        var session = SessionLoader.LoadFlagship();
        var snapshot = new SessionSnapshot(session);

        Assert.True(snapshot.HasSession);
        Assert.True(snapshot.HasPlayerTeam);
        Assert.NotEmpty(snapshot.TeamName);
        Assert.NotEmpty(snapshot.TeamBadge);
        Assert.True(snapshot.HasBoard);
        Assert.InRange(snapshot.BoardConfidence, 0, 100);
        Assert.Equal(session.Clock.Date, snapshot.Date);
    }

    [Fact]
    public void Snapshot_gates_off_without_a_player_team()
    {
        var session = SessionLoader.LoadFlagship();
        var noPlayer = session with { Carset = session.Carset with { PlayerTeamId = "" } };

        var snapshot = new SessionSnapshot(noPlayer);

        Assert.True(snapshot.HasSession);
        Assert.False(snapshot.HasPlayerTeam);
        Assert.False(snapshot.HasBoard);
        Assert.False(snapshot.HasCap);
    }

    [Fact]
    public void Loading_is_deterministic()
    {
        var a = SessionLoader.LoadFlagship();
        var b = SessionLoader.LoadFlagship();

        Assert.Equal(a.Clock.Date, b.Clock.Date);
        Assert.Equal(a.Seed, b.Seed);
    }
}
