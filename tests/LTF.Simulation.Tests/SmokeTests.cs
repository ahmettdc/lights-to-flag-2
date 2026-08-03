using Xunit;

namespace LTF.Simulation.Tests;

/// <summary>
/// Skeleton smoke test: proves the LTF.Simulation assembly builds, is referenced, and
/// runs under xUnit on Linux CI. Real tests arrive with the milestone that
/// populates this layer.
/// </summary>
public class SmokeTests
{
    [Fact]
    public void Layer_is_wired()
    {
        Assert.Equal("Simulation", LTF.Simulation.AssemblyInfo.Layer);
    }
}
