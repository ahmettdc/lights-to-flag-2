using Xunit;

namespace LTF.Career.Tests;

/// <summary>
/// Skeleton smoke test: proves the LTF.Career assembly builds, is referenced, and
/// runs under xUnit on Linux CI. Real tests arrive with the milestone that
/// populates this layer.
/// </summary>
public class SmokeTests
{
    [Fact]
    public void Layer_is_wired()
    {
        Assert.Equal("Career", LTF.Career.AssemblyInfo.Layer);
    }
}
