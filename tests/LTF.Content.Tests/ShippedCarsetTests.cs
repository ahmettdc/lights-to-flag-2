using LTF.Content;
using LTF.Domain.Racing;
using Xunit;

namespace LTF.Content.Tests;

/// <summary>
/// Loads the real carset files (copied next to the test assembly) and proves they parse
/// and validate cleanly on CI — the shipped sample and the real-2025 mod alike.
/// </summary>
public class ShippedCarsetTests
{
    public static TheoryData<string> Carsets => new()
    {
        "carsets/global-prix.json",
        "carsets/real-2025.json",
    };

    [Theory]
    [MemberData(nameof(Carsets))]
    public void Carset_loads_and_validates_cleanly(string relativePath)
    {
        var path = Path.Combine(AppContext.BaseDirectory, relativePath);
        Assert.True(File.Exists(path), $"missing content file: {path}");

        var carset = CarsetLoader.LoadFromJson(File.ReadAllText(path));

        Assert.Empty(CarsetValidator.Validate(carset));
        Assert.Equal(10, carset.Teams.Count);
        Assert.Equal(20, carset.Drivers.Count);
        Assert.Equal(24, carset.Circuits.Count);
        Assert.Equal(24, carset.Calendar.Count);
        Assert.Equal(6, carset.Calendar.Count(r => r.IsSprint));
    }

    [Fact]
    public void The_flagship_carset_is_the_drs_era()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "carsets/global-prix.json");
        var carset = CarsetLoader.LoadFromJson(File.ReadAllText(path));

        Assert.Equal(RegulationEra.DrsEra, carset.Regulations.Era);
    }

    [Fact]
    public void The_2026_carset_loads_in_the_active_aero_era()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "carsets/global-prix-2026.json");
        Assert.True(File.Exists(path), $"missing content file: {path}");

        var carset = CarsetLoader.LoadFromJson(File.ReadAllText(path));

        Assert.Empty(CarsetValidator.Validate(carset));
        Assert.Equal(RegulationEra.ActiveAero2026, carset.Regulations.Era);
        Assert.Equal(4, carset.Teams.Count);
        Assert.Equal(8, carset.Drivers.Count);
        Assert.Equal(4, carset.Circuits.Count);
    }
}
