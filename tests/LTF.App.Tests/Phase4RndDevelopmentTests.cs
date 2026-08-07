using System.IO;
using System.Linq;
using LTF.App.Services;
using LTF.App.Session;
using LTF.Career;
using LTF.Domain;
using LTF.Domain.Rnd;
using Xunit;

namespace LTF.App.Tests;

/// <summary>
/// Model B — live mid-season R&amp;D development (Ri1): the live career threads <see cref="RndProgression"/>
/// through every path (Continue, Reconstruct, RollToNextSeason), so the car evolves round by round within a
/// season, a mid-season reload reconstructs the same evolved car deterministically (no save-format change), a
/// carset with no tech tree stays untouched (inert/byte-identical), and the season's development carries into
/// the next season's base and survives a save/reload.
/// </summary>
public class Phase4RndDevelopmentTests
{
    private static int PlayerAero(Carset carset) => carset.PlayerTeam()!.Car.Aerodynamics.Value;

    private static void DriveSeason(LiveCareer live)
    {
        var guard = 0;
        while (!live.SeasonComplete && guard++ < 1000)
        {
            if (live.PendingAction)
            {
                live.Acknowledge();
            }
            else
            {
                live.Continue();
            }
        }
    }

    [Fact]
    public void A_live_season_develops_the_player_car_mid_season()
    {
        var session = SessionLoader.LoadFlagship();
        var live = new LiveCareer(session);
        var startAero = PlayerAero(live.SeasonStart);

        DriveSeason(live);

        var endAero = PlayerAero(live.Current);
        Assert.True(endAero > startAero, $"the player's aero should develop mid-season (start {startAero}, end {endAero})");
        Assert.NotSame(live.SeasonStart, live.Current); // the car evolved away from the pristine season base
    }

    [Fact]
    public void A_mid_season_reload_reconstructs_the_same_evolved_car_and_standings()
    {
        var session = SessionLoader.LoadFlagship();

        var live = new LiveCareer(session);
        var guard = 0;
        while (live.Results.Count < 5 && !live.SeasonComplete && guard++ < 1000)
        {
            if (live.PendingAction)
            {
                live.Acknowledge();
            }
            else
            {
                live.Continue();
            }
        }

        // Rebuild from (season-start carset, seed, the reached date) — the load path — with no persisted car.
        var resumed = new LiveCareer(session with { Clock = session.Clock with { Date = live.Clock.Date } });

        Assert.Equal(live.Results.Count, resumed.Results.Count);
        Assert.Equal(live.Standings.Drivers, resumed.Standings.Drivers);
        Assert.Equal(live.Standings.Constructors, resumed.Standings.Constructors);
        // The full evolved car is reconstructed, not just the standings — the car is deterministic in the seed.
        Assert.Equal(live.Current.PlayerTeam()!.Car, resumed.Current.PlayerTeam()!.Car);
    }

    [Fact]
    public void A_carset_without_a_tech_tree_leaves_the_car_untouched_all_season()
    {
        var session = SessionLoader.LoadFlagship();
        var inert = session.Carset with { TechTree = session.Carset.TechTree with { Nodes = [] } };
        var live = new LiveCareer(new ShellSession(inert, session.Clock, session.Seed));

        DriveSeason(live);

        Assert.Same(live.SeasonStart, live.Current); // no R&D → the carset is never evolved (inert/byte-identical)
        var expected = SeasonSimulator.Run(inert, session.Seed).Standings; // pristine Run matches (no mid-season dev)
        Assert.Equal(expected.Drivers, live.Standings.Drivers);
        Assert.Equal(expected.Constructors, live.Standings.Constructors);
    }

    [Fact]
    public void A_rollover_carries_the_evolved_car_forward_and_it_survives_a_reload()
    {
        var session = SessionLoader.LoadFlagship();
        var startAero = PlayerAero(session.Carset);
        var live = new LiveCareer(session);

        DriveSeason(live);
        if (live.PendingAction)
        {
            live.Acknowledge();
        }

        live.Continue(); // cross the boundary — roll into the next season on the evolved carset

        var rolledAero = PlayerAero(live.SeasonStart);
        Assert.True(rolledAero > startAero, $"the season's development should carry into the new base (start {startAero}, rolled {rolledAero})");

        // Save → reload reproduces the rolled base: the evolved car persists via the existing research capture,
        // so no save-format change is needed.
        var catalog = CarsetCatalog.Discover();
        var saves = new SaveStore(catalog, Directory.CreateTempSubdirectory().FullName);
        saves.Save(live.SaveSession);
        var reloaded = new LiveCareer(saves.Load(saves.MostRecent()!));

        Assert.Equal(rolledAero, PlayerAero(reloaded.SeasonStart));
        Assert.Equal(live.SeasonStart.PlayerTeam()!.Car, reloaded.SeasonStart.PlayerTeam()!.Car);
    }

    [Fact]
    public void Two_live_careers_from_the_same_seed_evolve_identically()
    {
        var a = new LiveCareer(SessionLoader.LoadFlagship());
        var b = new LiveCareer(SessionLoader.LoadFlagship());

        DriveSeason(a);
        DriveSeason(b);

        Assert.Equal(a.Current.PlayerTeam()!.Car, b.Current.PlayerTeam()!.Car);
        Assert.Equal(a.Standings.Drivers, b.Standings.Drivers);
        Assert.Equal(a.Standings.Constructors, b.Standings.Constructors);
    }
}
