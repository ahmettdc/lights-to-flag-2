using LTF.Domain.Common;
using Xunit;

namespace LTF.Domain.Tests;

public class FacilityLevelTests
{
    [Fact]
    public void A_level_keeps_its_value()
    {
        Assert.Equal(4, new FacilityLevel(4).Value);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(6)]
    public void An_out_of_range_level_throws(int value)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new FacilityLevel(value));
    }

    [Fact]
    public void Normalized_spans_zero_to_one()
    {
        Assert.Equal(0.0, new FacilityLevel(1).Normalized);
        Assert.Equal(0.5, new FacilityLevel(3).Normalized);
        Assert.Equal(1.0, new FacilityLevel(5).Normalized);
    }

    [Fact]
    public void Clamped_pins_into_range_instead_of_throwing()
    {
        Assert.Equal(1, FacilityLevel.Clamped(-3).Value);
        Assert.Equal(5, FacilityLevel.Clamped(99).Value);
        Assert.Equal(3, FacilityLevel.Clamped(3).Value);
    }

    [Fact]
    public void It_converts_implicitly_to_int()
    {
        int level = new FacilityLevel(4);
        Assert.Equal(4, level);
    }
}
