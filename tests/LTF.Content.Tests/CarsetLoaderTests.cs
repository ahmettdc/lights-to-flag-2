using LTF.Content;
using LTF.Domain.Racing;
using Xunit;

namespace LTF.Content.Tests;

public class CarsetLoaderTests
{
    [Fact]
    public void Loads_a_valid_minimal_carset()
    {
        var carset = CarsetLoader.LoadFromJson(TestData.MinimalValid);

        Assert.Equal("t", carset.Id);
        Assert.Single(carset.Teams);
        Assert.Equal(2, carset.Drivers.Count);
        Assert.Single(carset.Calendar);
        Assert.Equal(new DateOnly(2025, 3, 16), carset.Calendar[0].Date);
        Assert.Equal(80, carset.Teams[0].Car.Aerodynamics.Value);
    }

    [Fact]
    public void Rejects_invalid_json()
    {
        var ex = Assert.Throws<CarsetValidationException>(() => CarsetLoader.LoadFromJson("{ not json"));
        Assert.Contains("JSON", ex.Message);
    }

    [Fact]
    public void Reports_missing_required_field_with_path()
    {
        var json = TestData.MinimalValid.Replace("\"age\": 25,", "", StringComparison.Ordinal);
        var ex = Assert.Throws<CarsetValidationException>(() => CarsetLoader.LoadFromJson(json));
        Assert.Contains("drivers[0].age", ex.Message);
    }

    [Fact]
    public void Rejects_out_of_range_rating()
    {
        var json = TestData.MinimalValid.Replace("\"pace\": 80", "\"pace\": 150", StringComparison.Ordinal);
        var ex = Assert.Throws<CarsetValidationException>(() => CarsetLoader.LoadFromJson(json));
        Assert.Contains("pace", ex.Message);
    }

    [Fact]
    public void Rejects_unknown_enum_value()
    {
        var json = TestData.MinimalValid.Replace("\"compound\": \"Soft\"", "\"compound\": \"Ultra\"", StringComparison.Ordinal);
        var ex = Assert.Throws<CarsetValidationException>(() => CarsetLoader.LoadFromJson(json));
        Assert.Contains("compound", ex.Message);
    }

    [Fact]
    public void Rejects_a_badly_formatted_date()
    {
        var json = TestData.MinimalValid.Replace("2025-03-16", "03/16/2025", StringComparison.Ordinal);
        var ex = Assert.Throws<CarsetValidationException>(() => CarsetLoader.LoadFromJson(json));
        Assert.Contains("date", ex.Message);
    }

    // ---- Regulations (26c / ADR-0018) -------------------------------------

    [Fact]
    public void No_regulations_block_is_the_drs_era()
    {
        var carset = CarsetLoader.LoadFromJson(TestData.MinimalValid);
        Assert.Equal(RegulationEra.DrsEra, carset.Regulations.Era);
    }

    [Fact]
    public void Loads_the_2026_era_and_defaults_unspecified_params()
    {
        var json = TestData.MinimalValid.Replace(
            "\"name\": \"T\",",
            "\"name\": \"T\", \"regulations\": { \"era\": \"ActiveAero2026\" },",
            StringComparison.Ordinal);

        var carset = CarsetLoader.LoadFromJson(json);

        Assert.Equal(RegulationEra.ActiveAero2026, carset.Regulations.Era);
        Assert.Equal(new RegulationSet().ManualOverrideBoost, carset.Regulations.ManualOverrideBoost);
    }

    [Fact]
    public void Reads_2026_tuning_parameters()
    {
        var json = TestData.MinimalValid.Replace(
            "\"name\": \"T\",",
            "\"name\": \"T\", \"regulations\": { \"era\": \"ActiveAero2026\", \"manualOverrideBoost\": 0.9 },",
            StringComparison.Ordinal);

        var carset = CarsetLoader.LoadFromJson(json);

        Assert.Equal(0.9, carset.Regulations.ManualOverrideBoost);
    }

    [Fact]
    public void Rejects_an_unknown_regulation_era()
    {
        var json = TestData.MinimalValid.Replace(
            "\"name\": \"T\",",
            "\"name\": \"T\", \"regulations\": { \"era\": \"Nonsense\" },",
            StringComparison.Ordinal);

        var ex = Assert.Throws<CarsetValidationException>(() => CarsetLoader.LoadFromJson(json));
        Assert.Contains("regulations.era", ex.Message);
    }

    [Fact]
    public void Reads_leading_lap_and_most_laps_led_points()
    {
        var json = TestData.MinimalValid.Replace(
            "\"racePoints\": [25, 18, 15] }",
            "\"racePoints\": [25, 18, 15], \"leadingLapPoint\": 1, \"mostLapsLedPoint\": 5 }",
            StringComparison.Ordinal);

        var carset = CarsetLoader.LoadFromJson(json);

        Assert.Equal(1, carset.Rules.Points.LeadingLapPoint);
        Assert.Equal(5, carset.Rules.Points.MostLapsLedPoint);
    }
}
