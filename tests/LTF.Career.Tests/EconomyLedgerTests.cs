using System.Linq;
using LTF.Domain;
using LTF.Domain.Common;
using LTF.Domain.Management;
using LTF.Domain.Racing;
using Xunit;

namespace LTF.Career.Tests;

public class EconomyLedgerTests
{
    private static readonly long[] Prize = [100_000_000, 60_000_000];
    private const long Tv = 20_000_000;

    // A season carset with economy rules, per-team finances and one title sponsor, but no staff,
    // contracts or operating/crash cost — so a team's only money movement is income.
    private static Carset EconomyCarset(int rounds = 8)
    {
        var carset = CareerFixtures.SeasonCarset(rounds: rounds);
        var rules = carset.Rules with { Economy = new EconomyRules { PrizeMoney = Prize, TvIncome = Tv } };
        var teams = carset.Teams
            .Select(t => t with
            {
                Finances = new Finances { Balance = 100_000_000, CostCap = 135_000_000 },
                Sponsors =
                [
                    new Sponsor
                    {
                        Id = t.Id + "-title",
                        Name = "Title",
                        Tier = SponsorTier.Title,
                        PerRaceFee = 1_000_000,
                        PerPointBonus = 100_000,
                        ObjectiveBonus = 5_000_000,
                        ObjectivePosition = 1,
                    },
                ],
            })
            .ToList();
        return carset with { Rules = rules, Teams = teams };
    }

    [Fact]
    public void Prize_money_pays_by_constructor_position()
    {
        var carset = EconomyCarset();
        var season = SeasonSimulator.Run(carset, 7);

        var settled = EconomyLedger.SettleSeason(carset, season).Carset;

        Assert.Equal(1, season.Standings.Constructors.Single(c => c.TeamId == "alpha").Position); // alpha faster
        Assert.Equal(Prize[0], settled.Teams.Single(t => t.Id == "alpha").Finances.PrizeMoney);
        Assert.Equal(Prize[1], settled.Teams.Single(t => t.Id == "bravo").Finances.PrizeMoney);
    }

    [Fact]
    public void Income_credits_the_balance()
    {
        var carset = EconomyCarset();
        var season = SeasonSimulator.Run(carset, 7);

        var alpha = EconomyLedger.SettleSeason(carset, season).Carset.Teams.Single(t => t.Id == "alpha");

        // No expenses here, so the balance grew by exactly prize + TV + sponsor income.
        var income = alpha.Finances.PrizeMoney + Tv + alpha.Finances.SponsorIncome;
        Assert.Equal(100_000_000L + income, alpha.Finances.Balance);
        Assert.True(alpha.Finances.SponsorIncome > 0);
    }

    [Fact]
    public void The_objective_bonus_pays_only_when_its_position_is_met()
    {
        var carset = EconomyCarset();
        var season = SeasonSimulator.Run(carset, 7);
        var settled = EconomyLedger.SettleSeason(carset, season).Carset;

        // Objective is position 1: alpha (P1) earns the 5M bonus, bravo (P2) does not.
        var races = season.Rounds.Count;
        var alphaPoints = season.Standings.Constructors.Single(c => c.TeamId == "alpha").Points;
        var bravoPoints = season.Standings.Constructors.Single(c => c.TeamId == "bravo").Points;
        var alpha = settled.Teams.Single(t => t.Id == "alpha");
        var bravo = settled.Teams.Single(t => t.Id == "bravo");

        Assert.Equal(1_000_000L * races + 100_000L * alphaPoints + 5_000_000L, alpha.Finances.SponsorIncome);
        Assert.Equal(1_000_000L * races + 100_000L * bravoPoints, bravo.Finances.SponsorIncome);
    }

    [Fact]
    public void An_economy_free_carset_moves_no_money()
    {
        var carset = CareerFixtures.SeasonCarset(rounds: 4); // no economy, default finances
        var season = SeasonSimulator.Run(carset, 7);

        var settled = EconomyLedger.SettleSeason(carset, season).Carset;

        Assert.All(settled.Teams, t => Assert.Equal(0L, t.Finances.Balance));
    }

    [Fact]
    public void Settlement_is_deterministic()
    {
        var carset = EconomyCarset();
        var season = SeasonSimulator.Run(carset, 7);

        Assert.Equal(
            Key(EconomyLedger.SettleSeason(carset, season).Carset),
            Key(EconomyLedger.SettleSeason(carset, season).Carset));
    }

    private static string Key(Carset carset) =>
        string.Join(";", carset.Teams.Select(t =>
            $"{t.Id}:{t.Finances.Balance},{t.Finances.PrizeMoney},{t.Finances.SponsorIncome}"));
}
