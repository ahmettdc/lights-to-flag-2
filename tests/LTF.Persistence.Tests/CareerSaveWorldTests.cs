using System;
using System.Linq;
using LTF.Career;
using LTF.Domain;
using LTF.Domain.Racing;
using Xunit;

namespace LTF.Persistence.Tests;

public class CareerSaveWorldTests
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

    private static Car FlatCar(int v) => new()
    {
        Aerodynamics = new(v), Chassis = new(v), PowerUnit = new(v), TyreGentleness = new(v), Reliability = new(v),
    };

    private static RulesSet DevelopingRules() => new()
    {
        SeriesName = "S",
        Points = new PointsScheme { RacePoints = [25, 18] },
        DriverDevelopment = new DriverDevelopmentRules { PeakAgeStart = 27, PeakAgeEnd = 32, GrowthPerSeason = 0.5 },
    };

    // The freshly-loaded content: the original grid, no regen driver, no reserves.
    private static Carset Pristine() => new()
    {
        Id = "w",
        Name = "World",
        Rules = DevelopingRules(),
        Teams =
        [
            new Team { Id = "alpha", Name = "Alpha", Car = FlatCar(80), DriverIds = ["d1", "d2"] },
            new Team { Id = "bravo", Name = "Bravo", Car = FlatCar(75), DriverIds = ["d3", "d4"] },
        ],
        Drivers = [Driver("d1", 25, 70), Driver("d2", 25, 70), Driver("d3", 25, 70), Driver("d4", 25, 70)],
        Circuits = [],
        Calendar = [],
    };

    // A carset mid-career: alpha promoted a regen driver into d2's old seat, a rookie waits in reserve, and
    // d1 has aged into their thirties.
    private static Carset Evolved()
    {
        var pristine = Pristine();
        return pristine with
        {
            Teams = [pristine.Teams[0] with { DriverIds = ["d1", "regen-1"] }, pristine.Teams[1]],
            Drivers =
            [
                Driver("d1", age: 30, flat: 75, potential: 80),
                Driver("d3", age: 25, flat: 70),
                Driver("d4", age: 25, flat: 70),
                Driver("regen-1", age: 19, flat: 62, potential: 90),
            ],
            Reserves = [Driver("res1", age: 18, flat: 60, potential: 88)],
        };
    }

    [Fact]
    public void Restore_reconstructs_regen_drivers_moved_seats_and_ages()
    {
        var state = CareerState.Capture(Evolved(), new DateOnly(2027, 1, 1), 7);

        var restored = state.RestoreInto(Pristine());

        // The regen driver, absent from the pristine carset, is reconstructed from the snapshot.
        var regen = restored.Drivers.Single(d => d.Id == "regen-1");
        Assert.Equal(19, regen.Age);
        Assert.Equal(90, regen.Potential);

        // d1's evolved age survives (not reset to the pristine content's 25).
        Assert.Equal(30, restored.Drivers.Single(d => d.Id == "d1").Age);

        // The moved seat is restored: alpha now fields d1 and the regen driver, not d2.
        var alpha = restored.Teams.Single(t => t.Id == "alpha");
        Assert.Equal(new[] { "d1", "regen-1" }, alpha.DriverIds);

        // The reserve pool survives.
        Assert.Contains(restored.Reserves, d => d.Id == "res1");
    }

    [Fact]
    public void The_evolved_save_is_byte_stable()
    {
        var once = CareerStore.Serialize(CareerState.Capture(Evolved(), new DateOnly(2027, 1, 1), 7));
        var twice = CareerStore.Serialize(CareerStore.Deserialize(once));

        Assert.Equal(once, twice);
    }

    [Fact]
    public void A_non_developing_carset_captures_no_roster()
    {
        var pristine = Pristine();
        var noDev = pristine with { Rules = pristine.Rules with { DriverDevelopment = new DriverDevelopmentRules() } };

        var state = CareerState.Capture(noDev, new DateOnly(2027, 1, 1), 7);

        Assert.Empty(state.Roster);        // no development → the lighter id-keyed path, byte-identical
        Assert.Empty(state.ReservePool);
    }
}
