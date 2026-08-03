using LightsToFlag.Core.Simulation;

namespace LightsToFlag.Tests;

public class PracticeTests
{
    private static readonly Core.Domain.Coefficients Coeff = Fixtures.Coefficients();

    [Fact]
    public void More_practice_laps_improve_setup()
    {
        var competitor = Fixtures.Competitor();
        // Same seed for both so the noise draw is identical and only the lap count differs.
        var few = PracticeSimulator.ComputeSetup(competitor, Coeff, laps: 3, new SeededRandom(1));
        var many = PracticeSimulator.ComputeSetup(competitor, Coeff, laps: 40, new SeededRandom(1));
        Assert.True(many > few, $"many {many} should exceed few {few}");
    }

    [Fact]
    public void Better_feedback_raises_the_setup_ceiling()
    {
        var poor = Fixtures.Competitor(driver: Fixtures.Driver(smoothness: 5), team: Fixtures.Team(setup: 2));
        var good = Fixtures.Competitor(driver: Fixtures.Driver(smoothness: 5), team: Fixtures.Team(setup: 10));

        var poorSetup = PracticeSimulator.ComputeSetup(poor with { }, Coeff, laps: 200, new SeededRandom(2));
        var goodSetup = PracticeSimulator.ComputeSetup(good with { }, Coeff, laps: 200, new SeededRandom(2));
        Assert.True(goodSetup > poorSetup);
    }

    [Fact]
    public void Setup_is_bounded_to_unit_interval()
    {
        var competitor = Fixtures.Competitor(team: Fixtures.Team(setup: 10));
        var setup = PracticeSimulator.ComputeSetup(competitor, Coeff, laps: 10000, new SeededRandom(3));
        Assert.InRange(setup, 0.0, 1.0);
    }
}
