using System.IO;
using System.Linq;
using LightsToFlag.Core.Data;
using LightsToFlag.Core.Domain;

namespace LightsToFlag.Tests;

public class CarsetLoaderTests
{
    private static Carset LoadF1_2019()
    {
        var loader = new LegacyTextCarsetLoader();
        return loader.Load(TestCarsets.Path(TestCarsets.F1_2019));
    }

    [Fact]
    public void Loads_expected_entity_counts()
    {
        var carset = LoadF1_2019();

        Assert.Equal("F1 2019", carset.Name);
        // 20 full race drivers + 7 reserves (abbreviated rookie records).
        Assert.Equal(20, carset.Drivers.Count);
        Assert.Equal(7, carset.Reserves.Count);
        Assert.Equal(10, carset.Teams.Count);
        Assert.Equal(21, carset.Circuits.Count);
        Assert.Equal("Schumacher", carset.Reserves[0].LastName);
    }

    [Fact]
    public void Parses_rules_header_and_points()
    {
        var rules = LoadF1_2019().Rules;

        Assert.Equal("FIA", rules.GoverningBody);
        Assert.Equal("Formula 1", rules.SeriesName);
        Assert.Equal(CarType.Single, rules.CarType);
        Assert.Equal(20, rules.DriverCount);
        Assert.Equal(7, rules.SpareCount);
        Assert.Equal(21, rules.CircuitCount);
        Assert.Equal(1, rules.ClassCount);
        Assert.Equal(38, rules.RetirementAge);
        Assert.True(rules.KnockoutQualifying);

        // 2019 points table + the fastest-lap point.
        Assert.Equal(new double[] { 25, 18, 15, 12, 10, 8, 6, 4, 2, 1 }, rules.PrimaryPoints.ToArray());
        Assert.Empty(rules.SecondaryPoints);
        Assert.Equal(1d, rules.PointsForFastestLap);
        Assert.Equal(4, rules.EnginesPerSeason);
    }

    [Fact]
    public void Parses_first_driver_fields()
    {
        var vettel = LoadF1_2019().Drivers[0];

        Assert.Equal("Sebastian", vettel.FirstName);
        Assert.Equal("Vettel", vettel.LastName);
        Assert.Equal("Germany", vettel.Nationality);
        Assert.Equal(8, vettel.Pace);
        Assert.True(vettel.Championships >= 1);
    }

    [Fact]
    public void Parses_first_team_fields()
    {
        var redBull = LoadF1_2019().Teams[0];

        Assert.Equal("Red Bull Racing", redBull.Name);
        Assert.Equal("Aston Martin", redBull.TitleSponsor);
        Assert.Equal("Honda", redBull.EngineBrand);
        Assert.Equal(8, redBull.Aerodynamics);
        Assert.Equal(9, redBull.Resources);
        Assert.Equal(1, redBull.Class);
    }

    [Fact]
    public void Parses_first_circuit_including_variable_width_fields()
    {
        var albertPark = LoadF1_2019().Circuits[0];

        Assert.Equal("Australian", albertPark.Name);
        Assert.Equal(TrackKind.Track, albertPark.Kind);
        Assert.Equal(58, albertPark.Laps);
        Assert.Equal(84.125, albertPark.LapRecord, precision: 3);

        // One base laptime per class, then 6 corner + 3 overtaking strings.
        Assert.Single(albertPark.BaseLaptimeByClass);
        Assert.Equal(85d, albertPark.BaseLaptimeByClass[0]);
        Assert.Equal(6, albertPark.CornerStrings.Count);
        Assert.Equal(3, albertPark.OvertakingStrings.Count);
        Assert.All(albertPark.CornerStrings, s => Assert.False(string.IsNullOrWhiteSpace(s)));
    }

    [Fact]
    public void Missing_folder_throws_validation_error()
    {
        var loader = new LegacyTextCarsetLoader();
        var ex = Assert.Throws<CarsetValidationException>(
            () => loader.Load(Path.Combine(AppContext.BaseDirectory, "carsets", "Does Not Exist")));
        Assert.Contains("not found", ex.Message);
    }

    [Fact]
    public void Every_driver_references_a_valid_team()
    {
        var carset = LoadF1_2019();
        foreach (var driver in carset.Drivers)
        {
            Assert.InRange(driver.TeamNumber, 1, carset.Teams.Count);
        }
    }
}
