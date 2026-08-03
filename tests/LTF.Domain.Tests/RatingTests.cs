using LTF.Domain.Common;
using Xunit;

namespace LTF.Domain.Tests;

public class RatingTests
{
    [Theory]
    [InlineData(1)]
    [InlineData(50)]
    [InlineData(100)]
    public void Accepts_values_in_range(int value)
    {
        var rating = new Rating(value);
        Assert.Equal(value, rating.Value);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    [InlineData(101)]
    public void Rejects_values_out_of_range(int value)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new Rating(value));
    }

    [Theory]
    [InlineData(-20, 1)]
    [InlineData(250, 100)]
    [InlineData(73, 73)]
    public void Clamped_never_throws_and_bounds_the_value(int input, int expected)
    {
        Assert.Equal(expected, Rating.Clamped(input).Value);
    }

    [Fact]
    public void Normalized_maps_the_scale_onto_zero_to_one()
    {
        Assert.Equal(0.0, new Rating(Rating.Min).Normalized, 6);
        Assert.Equal(1.0, new Rating(Rating.Max).Normalized, 6);
    }

    [Fact]
    public void Converts_implicitly_to_int()
    {
        int value = new Rating(42);
        Assert.Equal(42, value);
    }
}
