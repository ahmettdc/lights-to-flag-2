using Xunit;

namespace LTF.Content.Tests;

/// <summary>
/// Skeleton smoke test: proves the LTF.Content assembly builds, is referenced, and
/// runs under xUnit on Linux CI. Real tests arrive with the milestone that
/// populates this layer.
/// </summary>
public class SmokeTests
{
    [Fact]
    public void Layer_is_wired()
    {
        Assert.Equal("Content", LTF.Content.AssemblyInfo.Layer);
    }
}
