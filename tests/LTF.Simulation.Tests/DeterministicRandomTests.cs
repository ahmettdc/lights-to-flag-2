using LTF.Simulation;
using Xunit;

namespace LTF.Simulation.Tests;

public class DeterministicRandomTests
{
    [Fact]
    public void Same_seed_produces_the_same_sequence()
    {
        var a = new DeterministicRandom(42);
        var b = new DeterministicRandom(42);
        for (var i = 0; i < 50; i++)
        {
            Assert.Equal(a.NextDouble(), b.NextDouble());
        }
    }

    [Fact]
    public void Different_seeds_diverge()
    {
        var a = new DeterministicRandom(1);
        var b = new DeterministicRandom(2);
        var differ = false;
        for (var i = 0; i < 50 && !differ; i++)
        {
            differ = a.NextDouble() != b.NextDouble();
        }

        Assert.True(differ);
    }

    [Fact]
    public void NextDouble_is_in_the_unit_interval()
    {
        var r = new DeterministicRandom(7);
        for (var i = 0; i < 5000; i++)
        {
            var x = r.NextDouble();
            Assert.True(x is >= 0.0 and < 1.0);
        }
    }

    [Fact]
    public void NextInt_stays_within_range()
    {
        var r = new DeterministicRandom(7);
        for (var i = 0; i < 5000; i++)
        {
            Assert.InRange(r.NextInt(5, 10), 5, 9);
        }
    }

    [Fact]
    public void Fork_is_reproducible_by_salt()
    {
        var f1 = new DeterministicRandom(7).Fork(3);
        var f2 = new DeterministicRandom(7).Fork(3);
        for (var i = 0; i < 20; i++)
        {
            Assert.Equal(f1.NextDouble(), f2.NextDouble());
        }
    }

    [Fact]
    public void Different_salts_give_different_streams()
    {
        var f1 = new DeterministicRandom(7).Fork(3);
        var f2 = new DeterministicRandom(7).Fork(4);
        var differ = false;
        for (var i = 0; i < 20 && !differ; i++)
        {
            differ = f1.NextDouble() != f2.NextDouble();
        }

        Assert.True(differ);
    }

    [Fact]
    public void Fork_is_independent_of_how_much_the_parent_consumed()
    {
        var parent = new DeterministicRandom(7);
        for (var i = 0; i < 100; i++)
        {
            parent.NextDouble();
        }

        var child = parent.Fork(3);
        var reference = new DeterministicRandom(7).Fork(3);
        for (var i = 0; i < 20; i++)
        {
            Assert.Equal(reference.NextDouble(), child.NextDouble());
        }
    }

    [Fact]
    public void Gaussian_is_approximately_standard_normal()
    {
        var r = new DeterministicRandom(123);
        const int n = 40000;
        double sum = 0, sumSquares = 0;
        for (var i = 0; i < n; i++)
        {
            var g = r.NextGaussian();
            sum += g;
            sumSquares += g * g;
        }

        var mean = sum / n;
        var stdDev = Math.Sqrt((sumSquares / n) - (mean * mean));

        Assert.InRange(mean, -0.05, 0.05);
        Assert.InRange(stdDev, 0.95, 1.05);
    }
}
