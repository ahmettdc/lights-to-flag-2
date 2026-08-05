using LTF.Domain.Racing;
using Xunit;

namespace LTF.Domain.Tests;

public class EconomyRulesTests
{
    [Fact]
    public void Prize_for_reads_the_table_by_position()
    {
        var economy = new EconomyRules { PrizeMoney = [100L, 60L, 40L] };

        Assert.Equal(100L, economy.PrizeFor(1));
        Assert.Equal(40L, economy.PrizeFor(3));
        Assert.Equal(0L, economy.PrizeFor(4)); // out of the paying positions
        Assert.Equal(0L, economy.PrizeFor(0)); // 1-based; 0 is invalid
    }

    [Fact]
    public void An_empty_economy_pays_and_costs_nothing()
    {
        var economy = new EconomyRules();

        Assert.Equal(0L, economy.PrizeFor(1));
        Assert.Equal(0L, economy.TvIncome);
        Assert.Equal(0L, economy.OperatingCostPerRace);
        Assert.Equal(0L, economy.CrashCostPerIncident);
        Assert.Empty(economy.PrizeMoney);
    }

    [Fact]
    public void Rules_default_to_an_empty_economy()
    {
        var rules = new RulesSet { SeriesName = "S", Points = new PointsScheme { RacePoints = [25] } };

        Assert.Empty(rules.Economy.PrizeMoney);
        Assert.Equal(0L, rules.Economy.TvIncome);
    }
}
