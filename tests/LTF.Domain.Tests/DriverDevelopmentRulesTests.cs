using LTF.Domain.Racing;
using Xunit;

namespace LTF.Domain.Tests;

public class DriverDevelopmentRulesTests
{
    [Fact]
    public void The_default_rules_are_inert()
    {
        var dev = new DriverDevelopmentRules();

        Assert.False(dev.IsActive);          // no growth, no decline → nobody develops
        Assert.Equal(0, dev.PeakAgeStart);
        Assert.Equal(0.0, dev.GrowthPerSeason);
    }

    [Theory]
    [InlineData(0.1, 0.0, 0.0)]
    [InlineData(0.0, 1.0, 0.0)]
    [InlineData(0.0, 0.0, 0.5)]
    public void Any_growth_or_decline_rate_activates_development(double growth, double physical, double experience)
    {
        var dev = new DriverDevelopmentRules
        {
            GrowthPerSeason = growth,
            PhysicalDeclinePerSeason = physical,
            ExperienceDeclinePerSeason = experience,
        };

        Assert.True(dev.IsActive);
    }

    [Fact]
    public void Peak_ages_alone_do_not_activate_development()
    {
        // A peak band with no rates still develops no one — the rates are what turn it on.
        var dev = new DriverDevelopmentRules { PeakAgeStart = 27, PeakAgeEnd = 32 };

        Assert.False(dev.IsActive);
    }

    [Fact]
    public void Rules_default_to_inert_development()
    {
        var rules = new RulesSet { SeriesName = "S", Points = new PointsScheme { RacePoints = [25] } };

        Assert.False(rules.DriverDevelopment.IsActive);
        Assert.Equal(0.0, rules.RegulationUnreadinessPenalty);
    }
}
