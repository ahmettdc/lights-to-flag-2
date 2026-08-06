using System;
using System.Linq;
using System.Reflection;
using Xunit;

namespace LTF.Architecture.Tests;

/// <summary>
/// Guards the ROADMAP §4.2 / ADR-0026 layering invariant from the engine side: none of the engine
/// assemblies (Content, Simulation, Career, Persistence) may depend on Avalonia or on the UI project
/// (LTF.App). Enforced by inspecting each compiled assembly's references, so an accidental <c>using</c>
/// that pulls the UI down into the engine fails the build in CI. LTF.Domain self-checks in its own tests.
/// </summary>
public class ArchitectureTests
{
    private static readonly string[] EngineAssemblies =
    {
        "LTF.Content",
        "LTF.Simulation",
        "LTF.Career",
        "LTF.Persistence",
    };

    [Fact]
    public void Engine_assemblies_do_not_reference_the_ui()
    {
        foreach (var name in EngineAssemblies)
        {
            var referenced = Assembly.Load(name)
                .GetReferencedAssemblies()
                .Select(a => a.Name ?? "")
                .ToList();

            Assert.False(
                referenced.Any(n => n.StartsWith("Avalonia", StringComparison.Ordinal)),
                $"{name} must not reference Avalonia (engine layers stay UI-free).");
            Assert.False(
                referenced.Any(n => n.StartsWith("LTF.App", StringComparison.Ordinal)),
                $"{name} must not reference LTF.App (engine layers stay UI-free).");
        }
    }
}
