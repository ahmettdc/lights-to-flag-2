using System.Linq;
using LTF.Simulation;
using Xunit;

namespace LTF.Simulation.Tests;

public class EntryListTests
{
    [Fact]
    public void Builds_a_competitor_for_every_seat()
    {
        var entries = EntryList.Build(SimFixtures.Carset());

        Assert.Equal(4, entries.Count);
        Assert.All(entries, e => Assert.False(string.IsNullOrEmpty(e.Id)));

        var alpha = entries.Where(e => e.TeamId == "alpha").ToList();
        Assert.Equal(2, alpha.Count);
        // Both alpha seats run the alpha car (flat 85 in the fixture).
        Assert.All(alpha, e => Assert.Equal(85, e.Car.Aerodynamics.Value));
    }

    [Fact]
    public void Throws_when_a_seat_references_an_unknown_driver()
    {
        var ex = Assert.Throws<InvalidOperationException>(
            () => EntryList.Build(SimFixtures.CarsetWithUnknownSeat()));
        Assert.Contains("ghost", ex.Message);
    }
}
