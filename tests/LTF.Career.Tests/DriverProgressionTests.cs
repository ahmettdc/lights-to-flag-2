using System;
using System.Linq;
using LTF.Domain;
using LTF.Domain.Racing;
using Xunit;

namespace LTF.Career.Tests;

public class DriverProgressionTests
{
    private static DriverAttributes Flat(int v) => new()
    {
        Pace = new(v), Racecraft = new(v), Consistency = new(v),
        TyreManagement = new(v), WetWeather = new(v), Feedback = new(v),
    };

    private static Driver Driver(string id, int age, int flat, int potential = 0) => new()
    {
        Id = id, FirstName = id, LastName = "D", Age = age, Attributes = Flat(flat), Potential = potential,
    };

    // A young high-potential driver and an over-the-hill veteran, on a curve that grows before 27 and
    // declines after 32 (physical three points a year, experience one).
    private static Carset DevelopmentCarset(double spread = 0.0)
    {
        var carset = CareerFixtures.Carset();
        var rules = carset.Rules with
        {
            DriverDevelopment = new DriverDevelopmentRules
            {
                PeakAgeStart = 27,
                PeakAgeEnd = 32,
                GrowthPerSeason = 0.5,
                PhysicalDeclinePerSeason = 3.0,
                ExperienceDeclinePerSeason = 1.0,
                DevelopmentSpread = spread,
            },
        };
        return carset with
        {
            Rules = rules,
            Drivers =
            [
                Driver("young", age: 20, flat: 60, potential: 90),
                Driver("vet", age: 35, flat: 70),
            ],
        };
    }

    private static Driver Find(Carset carset, string id) => carset.Drivers.Single(d => d.Id == id);

    [Fact]
    public void A_young_driver_grows_and_a_veteran_declines_physical_first()
    {
        var advanced = DriverProgression.Advance(DevelopmentCarset(), seed: 7);

        var young = Find(advanced, "young");
        Assert.Equal(21, young.Age);                     // everyone ages a year
        Assert.True(young.Attributes.Overall > 60);      // grew toward the 90 ceiling

        var vet = Find(advanced, "vet");
        Assert.Equal(36, vet.Age);
        var pace = 70 - vet.Attributes.Pace.Value;       // physical drop
        var racecraft = 70 - vet.Attributes.Racecraft.Value; // experience drop
        Assert.True(pace > 0);                           // the veteran declines
        Assert.True(pace > racecraft);                   // physical fades faster than experience
    }

    [Fact]
    public void A_carset_without_a_development_curve_is_returned_untouched()
    {
        var carset = CareerFixtures.SeasonCarset(rounds: 2); // no driverDevelopment → inert

        Assert.Same(carset, DriverProgression.Advance(carset, 7));
    }

    [Fact]
    public void Development_is_deterministic()
    {
        var carset = DevelopmentCarset(spread: 0.4);

        Assert.Equal(Key(DriverProgression.Advance(carset, 7)), Key(DriverProgression.Advance(carset, 7)));
    }

    private static string Key(Carset carset) => string.Join("|", carset.Drivers
        .OrderBy(d => d.Id, StringComparer.Ordinal)
        .Select(d =>
        {
            var a = d.Attributes;
            return $"{d.Id}:{d.Age}:{a.Pace.Value}/{a.Racecraft.Value}/{a.Consistency.Value}/" +
                   $"{a.TyreManagement.Value}/{a.WetWeather.Value}/{a.Feedback.Value}";
        }));
}
