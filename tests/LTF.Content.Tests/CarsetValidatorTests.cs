using LTF.Content;
using Xunit;

namespace LTF.Content.Tests;

public class CarsetValidatorTests
{
    [Fact]
    public void Valid_minimal_carset_has_no_issues()
    {
        var carset = CarsetLoader.LoadFromJson(TestData.MinimalValid);
        Assert.Empty(CarsetValidator.Validate(carset));
    }

    [Fact]
    public void Unknown_driver_reference_is_an_error()
    {
        var json = TestData.MinimalValid.Replace(
            "\"driverIds\": [ \"d1\", \"d2\" ]", "\"driverIds\": [ \"d1\", \"dX\" ]", StringComparison.Ordinal);
        var issues = CarsetValidator.Validate(CarsetLoader.LoadFromJson(json));

        Assert.Contains(issues, i =>
            i.Severity == ValidationSeverity.Error && i.Message.Contains("unknown driver 'dX'", StringComparison.Ordinal));
    }

    [Fact]
    public void Driver_seated_nowhere_is_a_warning()
    {
        var json = TestData.MinimalValid.Replace(
            "\"driverIds\": [ \"d1\", \"d2\" ]", "\"driverIds\": [ \"d1\", \"dX\" ]", StringComparison.Ordinal);
        var issues = CarsetValidator.Validate(CarsetLoader.LoadFromJson(json));

        Assert.Contains(issues, i =>
            i.Severity == ValidationSeverity.Warning && i.Message.Contains("'d2'", StringComparison.Ordinal));
    }

    [Fact]
    public void Calendar_referencing_unknown_circuit_is_an_error()
    {
        var json = TestData.MinimalValid.Replace(
            "\"circuitId\": \"c1\"", "\"circuitId\": \"cX\"", StringComparison.Ordinal);
        var issues = CarsetValidator.Validate(CarsetLoader.LoadFromJson(json));

        Assert.Contains(issues, i =>
            i.Severity == ValidationSeverity.Error && i.Message.Contains("unknown circuit 'cX'", StringComparison.Ordinal));
    }

    [Fact]
    public void Negative_regulation_coefficient_is_an_error()
    {
        var json = TestData.MinimalValid.Replace(
            "\"name\": \"T\",",
            "\"name\": \"T\", \"regulations\": { \"era\": \"ActiveAero2026\", \"energyRegenPerLap\": -0.5 },",
            StringComparison.Ordinal);
        var issues = CarsetValidator.Validate(CarsetLoader.LoadFromJson(json));

        Assert.Contains(issues, i =>
            i.Severity == ValidationSeverity.Error && i.Message.Contains("non-negative", StringComparison.Ordinal));
    }

    [Fact]
    public void Out_of_range_derating_threshold_is_an_error()
    {
        var json = TestData.MinimalValid.Replace(
            "\"name\": \"T\",",
            "\"name\": \"T\", \"regulations\": { \"era\": \"ActiveAero2026\", \"deRatingThreshold\": 1.5 },",
            StringComparison.Ordinal);
        var issues = CarsetValidator.Validate(CarsetLoader.LoadFromJson(json));

        Assert.Contains(issues, i =>
            i.Severity == ValidationSeverity.Error && i.Message.Contains("deRatingThreshold", StringComparison.Ordinal));
    }
}
