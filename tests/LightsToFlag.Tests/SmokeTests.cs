using System.Linq;
using LightsToFlag.Core.Domain;

namespace LightsToFlag.Tests;

public class SmokeTests
{
    [Fact]
    public void Core_is_referenced_and_builds()
    {
        Assert.Equal("F1 2019", TestCarsets.F1_2019);
    }

    /// <summary>
    /// Guards the split that keeps the engine testable on Linux CI: Core must not
    /// pull in any WPF / Windows presentation assembly, or it could no longer be
    /// built and tested on a non-Windows machine.
    /// </summary>
    [Fact]
    public void Core_does_not_reference_wpf_or_windows_assemblies()
    {
        var forbidden = new[] { "PresentationFramework", "PresentationCore", "WindowsBase", "System.Windows.Forms" };
        var referenced = typeof(Carset).Assembly
            .GetReferencedAssemblies()
            .Select(a => a.Name)
            .ToArray();

        foreach (var name in forbidden)
        {
            Assert.DoesNotContain(name, referenced);
        }
    }
}
