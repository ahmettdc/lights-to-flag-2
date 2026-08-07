using System.IO;
using System.Linq;
using LTF.App.Services;
using LTF.App.Session;
using LTF.Domain.Management;
using Xunit;

namespace LTF.App.Tests;

/// <summary>
/// The live management settle chain (M22a): the season-boundary rollover now settles the economy, services
/// bank loans, runs the enforcement (icra) ladder and reviews the board before evolving the world — so the
/// live career's finances and boards actually move, and a defaulting loan halts Continue with an
/// action-required inbox item (Rev 15). Golden + byte-identity stay intact (finances/boards bake into the
/// new SeasonStart and round-trip whole).
/// </summary>
public class Phase4SettleChainTests
{
    private static void DriveToSeasonEnd(LiveCareer live)
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
    public void The_rollover_now_settles_the_economy_and_stays_continuable()
    {
        var session = SessionLoader.LoadFlagship();
        var pristineBalance = session.Carset.PlayerTeam()!.Finances.Balance;
        var live = new LiveCareer(session);

        DriveToSeasonEnd(live);
        live.Continue(); // cross the boundary

        // Economy now settles live — the player balance moved from its season-one value.
        Assert.NotEqual(pristineBalance, live.SeasonStart.PlayerTeam()!.Finances.Balance);
        // Behaviour preserved for a no-loan career: still endless, still a new-season item, no enforcement halt.
        Assert.True(live.CanContinue);
        Assert.Contains(live.LastStepNews, n => n.Id.StartsWith("season-"));
        Assert.DoesNotContain(live.LastStepNews, n => n.Id.StartsWith("enforce-"));
    }

    [Fact]
    public void A_defaulting_loan_halts_continue_with_an_action_required_item()
    {
        var session = SessionLoader.LoadFlagship();
        var player = session.Carset.PlayerTeam()!;
        var loan = new Loan
        {
            Id = "loan-1", Principal = 10_000_000_000, AnnualRatePercent = 10,
            TermSeasons = 5, SeasonsRemaining = 5, OutstandingBalance = 10_000_000_000,
        };
        var indebted = player with { Finances = player.Finances with { Balance = 1_000_000, Loans = [loan] } };
        var teams = session.Carset.Teams.Select(t => t.Id == player.Id ? indebted : t).ToList();
        var carset = session.Carset with { Teams = teams };
        var live = new LiveCareer(new ShellSession(carset, session.Clock, session.Seed));

        DriveToSeasonEnd(live);
        live.Continue(); // cross the boundary — the unpayable instalment is missed

        Assert.False(live.CanContinue);        // Continue halts (Rev 15)
        Assert.True(live.PendingAction);
        Assert.Contains(live.LastStepNews, n => n.Id.StartsWith("enforce-") && n.RequiresAction);
        // The loan was serviced and the miss recorded on the rolled carset.
        var rolledLoan = live.SeasonStart.PlayerTeam()!.Finances.Loans.Single();
        Assert.True(rolledLoan.MissedPayments >= 1);
    }

    [Fact]
    public void Settled_finances_survive_a_save_and_reload()
    {
        var catalog = CarsetCatalog.Discover();
        var dir = Directory.CreateTempSubdirectory().FullName;
        var saves = new SaveStore(catalog, dir);

        var live = new LiveCareer(SessionLoader.LoadFlagship());
        DriveToSeasonEnd(live);
        live.Continue(); // cross a boundary so finances + boards are settled into the new SeasonStart

        saves.Save(live.SaveSession);
        var reloaded = saves.Load(saves.MostRecent()!);

        // The settled balance and evolved board confidence bake into SeasonStart and round-trip whole.
        Assert.Equal(
            live.SeasonStart.PlayerTeam()!.Finances.Balance,
            reloaded.Carset.PlayerTeam()!.Finances.Balance);
        Assert.Equal(
            live.SeasonStart.Boards.Single(b => b.TeamId == live.SeasonStart.PlayerTeamId).Metrics.BoardConfidence.Value,
            reloaded.Carset.Boards.Single(b => b.TeamId == reloaded.Carset.PlayerTeamId).Metrics.BoardConfidence.Value);
    }
}
