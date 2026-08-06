using System.Linq;
using LTF.Domain;
using LTF.Domain.Racing;
using Xunit;

namespace LTF.Career.Tests;

public class WorldSweepTests
{
    private static DriverAttributes Flat(int v) => new()
    {
        Pace = new(v), Racecraft = new(v), Consistency = new(v),
        TyreManagement = new(v), WetWeather = new(v), Feedback = new(v),
    };

    private static Driver Reserve(string id) => new()
    {
        Id = id, FirstName = id, LastName = "R", Age = 20, Attributes = Flat(66), Potential = 85,
    };

    // A developing world: drivers age, a low retirement age forces a veteran out after a season, and a
    // rookie waits in reserve to take the vacated seat.
    private static Carset WorldCarset()
    {
        var carset = CareerFixtures.SeasonCarset(rounds: 4);
        var rules = carset.Rules with
        {
            RetirementAge = 34,
            DriverDevelopment = new DriverDevelopmentRules
            {
                PeakAgeStart = 27,
                PeakAgeEnd = 32,
                GrowthPerSeason = 0.5,
                PhysicalDeclinePerSeason = 3.0,
                ExperienceDeclinePerSeason = 1.0,
            },
        };
        var drivers = carset.Drivers.Select(d => d.Id == "d1" ? d with { Age = 33 } : d).ToList();
        return carset with { Rules = rules, Drivers = drivers, Reserves = [Reserve("res1")] };
    }

    [Fact]
    public void A_developing_world_ages_retires_and_refills_the_grid()
    {
        var report = WorldSweep.Run(WorldCarset(), seasons: 4, seed: 7);

        Assert.True(report.Retirements >= 1);   // the 33-year-old ages out
        Assert.True(report.Debuts >= 1);        // and is replaced from the pool
        Assert.True(report.AllSeatsFilled);     // every team keeps a full line-up
        Assert.True(report.OldestAge < 34);     // no one races past the retirement age
        Assert.True(report.YoungestAge >= 18);  // rookies enter young
    }

    [Fact]
    public void An_unconfigured_world_neither_ages_nor_moves_anyone()
    {
        var report = WorldSweep.Run(CareerFixtures.SeasonCarset(rounds: 4), seasons: 3, seed: 7);

        Assert.Equal(0, report.Retirements);
        Assert.Equal(0, report.Debuts);
        Assert.Equal(25, report.OldestAge);   // no development → everyone stays 25
        Assert.Equal(25, report.YoungestAge);
    }

    [Fact]
    public void The_world_sweep_is_deterministic()
    {
        var carset = WorldCarset();

        Assert.Equal(Key(WorldSweep.Run(carset, 4, 7)), Key(WorldSweep.Run(carset, 4, 7)));
    }

    private static string Key(WorldSweepReport r) =>
        $"{r.Retirements}:{r.Debuts}:{r.OldestAge}:{r.YoungestAge}:" +
        string.Join("|", r.Teams.Select(t => $"{t.TeamId}={t.FinalOverall}/[{string.Join(",", t.DriverIds)}]"));
}
