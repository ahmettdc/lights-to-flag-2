using LTF.Content;
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
}
