using LTF.App.Converters;
using Xunit;

namespace LTF.App.Tests;

/// <summary>
/// The status-colour rule (>= 80 green, >= 70 amber, else red) is the shell's most reused primitive,
/// so it gets exhaustive boundary coverage. Pure logic — no headless app needed.
/// </summary>
public class StatusColorConverterTests
{
    [Theory]
    [InlineData(100.0)]
    [InlineData(80.0)] // boundary: good starts exactly at 80
    public void At_or_above_80_is_good(double value) =>
        Assert.Same(StatusColorConverter.Good, StatusColorConverter.Classify(value));

    [Theory]
    [InlineData(79.9)]
    [InlineData(70.0)] // boundary: warn starts exactly at 70
    public void Between_70_and_80_is_warn(double value) =>
        Assert.Same(StatusColorConverter.Warn, StatusColorConverter.Classify(value));

    [Theory]
    [InlineData(69.9)]
    [InlineData(0.0)]
    [InlineData(-5.0)]
    public void Below_70_is_bad(double value) =>
        Assert.Same(StatusColorConverter.Bad, StatusColorConverter.Classify(value));

    [Fact]
    public void Integer_values_are_classified() =>
        Assert.Same(StatusColorConverter.Good, StatusColorConverter.Classify(90));

    [Fact]
    public void Null_and_nan_fall_back_to_bad()
    {
        Assert.Same(StatusColorConverter.Bad, StatusColorConverter.Classify(null));
        Assert.Same(StatusColorConverter.Bad, StatusColorConverter.Classify(double.NaN));
    }
}
