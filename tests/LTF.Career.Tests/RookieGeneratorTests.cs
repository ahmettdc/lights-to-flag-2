using LTF.Domain.Racing;
using Xunit;

namespace LTF.Career.Tests;

public class RookieGeneratorTests
{
    [Fact]
    public void Generates_a_valid_in_range_rookie_with_upside()
    {
        var rookie = RookieGenerator.Generate(seed: 7, id: "regen-1");

        Assert.Equal("regen-1", rookie.Id);
        Assert.NotEmpty(rookie.FirstName);
        Assert.NotEmpty(rookie.LastName);
        Assert.NotEmpty(rookie.Nationality);
        Assert.InRange(rookie.Age, 18, 22);

        var a = rookie.Attributes;
        foreach (var value in new[]
                 {
                     a.Pace.Value, a.Racecraft.Value, a.Consistency.Value,
                     a.TyreManagement.Value, a.WetWeather.Value, a.Feedback.Value,
                 })
        {
            Assert.InRange(value, 1, 100);
        }

        Assert.True(rookie.Potential >= a.Overall);  // a rookie has room to grow
        Assert.True(rookie.Potential <= 100);
    }

    [Fact]
    public void The_same_seed_and_id_generate_an_identical_rookie()
    {
        Assert.Equal(RookieGenerator.Generate(7, "regen-1"), RookieGenerator.Generate(7, "regen-1"));
    }

    [Fact]
    public void A_different_seed_generates_a_different_rookie()
    {
        // Same id, different seed → the forked stream diverges, so the invented driver differs.
        Assert.NotEqual(RookieGenerator.Generate(7, "regen-1"), RookieGenerator.Generate(8, "regen-1"));
    }
}
